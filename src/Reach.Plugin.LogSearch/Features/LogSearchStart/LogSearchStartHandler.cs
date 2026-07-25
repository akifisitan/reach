using Reach.Plugin.Abstractions;
using Reach.Relay.Abstractions;

namespace Reach.Plugin.LogSearch.Features.LogSearchStart;

internal sealed class LogSearchStartHandler
    : RpcHandler<LogSearchStartPayload, LogSearchStartResult>
{
    private readonly ILogger<LogSearchStartHandler> _logger;
    private readonly ILocalRelay _relay;
    private readonly SearchResultStore _searchResultStore;

    public LogSearchStartHandler(
        ILogger<LogSearchStartHandler> logger,
        ILocalRelay relay,
        SearchResultStore searchResultStore
    )
    {
        _logger = logger;
        _relay = relay;
        _searchResultStore = searchResultStore;
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

            _searchResultStore.Insert(payload.CorrelationId); // TODO: insert, if it fails because of duplicate, error saying to check the status as it already exists

            var result = await _relay.PostToRemote(command, cancellationToken);

            _searchResultStore.Insert(payload.CorrelationId);

            _logger.ZLogInformation($"[{command.Id}] Posting successful {result.PollId.Value}");

            if (Random.Shared.Next(0, 2) == 1)
            {
                throw new Exception("Threw an error just for testing");
            }

            return new LogSearchStartResult.Success(payload.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"An error occurred");

            return new LogSearchStartResult.Error(ex.Message);
        }
    }
}
