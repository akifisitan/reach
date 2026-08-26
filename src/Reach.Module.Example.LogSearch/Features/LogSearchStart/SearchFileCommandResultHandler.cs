using Reach.Module.Toolkit.ResultStore;

namespace Reach.Module.Example.LogSearch.Features.LogSearchStart;

internal sealed class SearchFileCommandResultHandler
    : RelayCommandResultHandler<SearchFileCommandResult>
{
    private readonly ILogger<SearchFileCommandResultHandler> _logger;
    private readonly IResultStore<SearchFileCommandResult> _resultStore;

    public SearchFileCommandResultHandler(
        ILogger<SearchFileCommandResultHandler> logger,
        IResultStore<SearchFileCommandResult> resultStore
    )
    {
        _logger = logger;
        _resultStore = resultStore;
    }

    protected override async Task<Unit> Handle(
        SearchFileCommandResult result,
        CancellationToken cancellationToken
    )
    {
        _logger.ZLogInformation($"Received search result for id: {result.CommandId}");

        if (!await _resultStore.TrySetAsComplete(result, cancellationToken).ConfigureAwait(false))
        {
            _logger.ZLogError($"Result with id: {result.CommandId} not found");

            return new();
        }

        _logger.ZLogInformation($"Received value from remote successfully. Result: {result}");

        return new();
    }
}
