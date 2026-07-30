using Reach.Module.Toolkit.ResultStore;
using Reach.Relay;

namespace Reach.Module.LogSearch.Features.LogSearchStart;

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

            var result = await _relay.PostToRemote(command, cancellationToken);

            _logger.ZLogInformation($"[{command.Id}] Posting successful {result.PollId.Value}");

            return new LogSearchStartResult.Success(id);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"An error occurred");

            await _resultStore.TrySetAsFailed(id, ex.Message, cancellationToken);

            return new LogSearchStartResult.Error(ex.Message);
        }
    }
}
