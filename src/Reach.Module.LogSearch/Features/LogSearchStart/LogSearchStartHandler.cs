using Polly;
using Polly.Registry;
using Reach.Module.Toolkit.ResultStore;
using Reach.Relay;

namespace Reach.Module.LogSearch.Features.LogSearchStart;

internal sealed class LogSearchStartHandler
    : RpcHandler<LogSearchStartPayload, LogSearchStartResult>
{
    private readonly ILogger<LogSearchStartHandler> _logger;
    private readonly ILocalRelay _relay;
    private readonly ResiliencePipeline _relayPostChaos;
    private readonly IResultStore<SearchFileCommandResult> _resultStore;

    public LogSearchStartHandler(
        ILogger<LogSearchStartHandler> logger,
        ILocalRelay relay,
        IResultStore<SearchFileCommandResult> resultStore,
        ResiliencePipelineProvider<string> resiliencePipelineProvider
    )
    {
        _logger = logger;
        _relay = relay;
        _resultStore = resultStore;
        _relayPostChaos = resiliencePipelineProvider.GetPipeline(ChaosPolicyNames.RelayPost);
    }

    protected override async Task<LogSearchStartResult> Handle(
        LogSearchStartPayload payload,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var command = new SearchFileCommand(
                payload.CorrelationId,
                payload.Query,
                payload.ServerName,
                payload.StartTime,
                payload.EndTime
            );

            _logger.ZLogInformation($"[{command.Id}] Posting {command} to remote");

            await _resultStore.Insert(payload.CorrelationId, cancellationToken);

            var result = await _relayPostChaos.ExecuteAsync(
                async ct => await _relay.PostToRemote(command, ct),
                cancellationToken
            );

            _logger.ZLogInformation($"[{command.Id}] Posting successful {result.PollId.Value}");

            return new LogSearchStartResult.Success(payload.CorrelationId);
        }
        catch (ResultAlreadyExistsException ex)
        {
            _logger.ZLogError(ex, $"[{payload.CorrelationId}] Search already exists");

            return new LogSearchStartResult.Error(
                $"A search with correlation id '{payload.CorrelationId}' already exists. Check its status."
            );
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"An error occurred");

            await _resultStore.TrySetAsFailed(payload.CorrelationId, ex.Message, cancellationToken);

            return new LogSearchStartResult.Error(ex.Message);
        }
    }
}
