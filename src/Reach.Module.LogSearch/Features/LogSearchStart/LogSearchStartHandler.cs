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
        var id = Guid.CreateVersion7().ToString("N");

        try
        {
            var command = new SearchFileCommand(
                id,
                payload.Query,
                payload.ServerName,
                payload.StartTime,
                payload.EndTime
            );

            _logger.ZLogInformation($"[{command.Id}] Posting {command} to remote");

            await _resultStore.Insert(id, cancellationToken);

            var result = await _relayPostChaos.ExecuteAsync(
                async ct => await _relay.PostToRemote(command, ct),
                cancellationToken
            );

            _logger.ZLogInformation($"[{command.Id}] Posting successful {result.PollId.Value}");

            return new LogSearchStartResult.Success(id);
        }
        catch (ResultAlreadyExistsException ex)
        {
            _logger.ZLogError(ex, $"[{id}] Search already exists");

            return new LogSearchStartResult.Error(
                $"A search with correlation id '{id}' already exists. Check its status."
            );
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"An error occurred");

            await _resultStore.TrySetAsFailed(id, ex.Message, cancellationToken);

            return new LogSearchStartResult.Error(ex.Message);
        }
    }
}
