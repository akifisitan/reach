using Reach.Plugin.Abstractions;
using Reach.Plugin.Toolkit.ResultStore.Abstractions;
using Reach.Relay.Abstractions;

namespace Reach.Plugin.LogSearch.Features.LogSearchStart;

internal sealed class LogSearchStartHandler
    : RpcHandler<LogSearchStartPayload, LogSearchStartResult>
{
    private readonly ILogger<LogSearchStartHandler> _logger;
    private readonly ILocalRelay _relay;
    private readonly IResultStore<SearchFileCommandResult> _resultStore;

    public LogSearchStartHandler(
        ILogger<LogSearchStartHandler> logger,
        ILocalRelay relay,
        IResultStore<SearchFileCommandResult> resultStore
    )
    {
        _logger = logger;
        _relay = relay;
        _resultStore = resultStore;
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

            var result = await _relay.PostToRemote(command, cancellationToken);

            _logger.ZLogInformation($"[{command.Id}] Posting successful {result.PollId.Value}");

            // TODO: inject random errors with Polly
            if (Random.Shared.Next(0, 2) == 1)
            {
                throw new Exception("Threw an error just for testing");
            }

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
