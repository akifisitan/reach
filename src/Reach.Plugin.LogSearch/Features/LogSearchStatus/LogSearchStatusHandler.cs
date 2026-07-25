using Reach.Plugin.Abstractions;

namespace Reach.Plugin.LogSearch.Features.LogSearchStatus;

internal sealed class LogSearchStatusHandler
    : RpcHandler<LogSearchStatusPayload, LogSearchStatusResult>
{
    private readonly ILogger<LogSearchStatusHandler> _logger;
    private readonly SearchResultStore _searchResultStore;

    public LogSearchStatusHandler(
        ILogger<LogSearchStatusHandler> logger,
        SearchResultStore searchResultStore
    )
    {
        _logger = logger;
        _searchResultStore = searchResultStore;
    }

    protected override async Task<LogSearchStatusResult> Handle(
        LogSearchStatusPayload payload,
        CancellationToken cancellationToken
    )
    {
        var storedValue = await _searchResultStore.GetValue(
            payload.CorrelationId,
            cancellationToken
        );

        if (storedValue is null)
        {
            return new LogSearchStatusResult.NotFound();
        }

        if (storedValue.Result is not null)
        {
            return new LogSearchStatusResult.Success(new(storedValue.Result.Content));
        }

        if (storedValue.Status is SearchStatus.Failed)
        {
            return new LogSearchStatusResult.Error(storedValue.ErrorMessage ?? "Search failed");
        }

        if (DateTimeOffset.Now - storedValue.CreationTime > _searchResultStore.StalenessThreshold)
        {
            return new LogSearchStatusResult.Expired();
        }

        return new LogSearchStatusResult.Ongoing();
    }
}
