using Reach.Plugin.Abstractions;
using Reach.Plugin.LogSearch.Features.LogSearchStart;
using Reach.Plugin.Toolkit.ResultStore.Abstractions;

namespace Reach.Plugin.LogSearch.Features.LogSearchStatus;

internal sealed class LogSearchStatusHandler
    : RpcHandler<LogSearchStatusPayload, LogSearchStatusResult>
{
    private readonly ILogger<LogSearchStatusHandler> _logger;
    private readonly IResultStore<SearchFileCommandResult> _resultStore;

    public LogSearchStatusHandler(
        ILogger<LogSearchStatusHandler> logger,
        IResultStore<SearchFileCommandResult> resultStore
    )
    {
        _logger = logger;
        _resultStore = resultStore;
    }

    protected override async Task<LogSearchStatusResult> Handle(
        LogSearchStatusPayload payload,
        CancellationToken cancellationToken
    )
    {
        var storedValue = await _resultStore.GetValue(payload.CorrelationId, cancellationToken);

        if (storedValue is null)
        {
            return new LogSearchStatusResult.NotFound();
        }

        if (storedValue.Result is not null)
        {
            return new LogSearchStatusResult.Success(new(storedValue.Result.Content));
        }

        if (storedValue.Status is ResultStoreStatus.Failed)
        {
            return new LogSearchStatusResult.Error(storedValue.ErrorMessage ?? "Search failed");
        }

        if (DateTimeOffset.Now - storedValue.CreationTime > _resultStore.StalenessThreshold)
        {
            return new LogSearchStatusResult.Expired();
        }

        return new LogSearchStatusResult.Ongoing();
    }
}
