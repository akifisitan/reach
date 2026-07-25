using Reach.Plugin.Abstractions;

namespace Reach.Plugin.LogSearch.Features.LogSearchStart;

internal sealed class SearchFileCommandResultHandler
    : RelayCommandResultHandler<SearchFileCommandResult>
{
    private readonly ILogger<SearchFileCommandResultHandler> _logger;
    private readonly SearchResultStore _searchResultStore;

    public SearchFileCommandResultHandler(
        ILogger<SearchFileCommandResultHandler> logger,
        SearchResultStore searchResultStore
    )
    {
        _logger = logger;
        _searchResultStore = searchResultStore;
    }

    protected override async Task<Unit> Handle(
        SearchFileCommandResult result,
        CancellationToken cancellationToken
    )
    {
        _logger.ZLogInformation($"Doing something with command: {result.Id}");

        if (Random.Shared.Next(0, 2) == 1)
        {
            throw new Exception($"Threw an error for testing");
        }

        if (!_searchResultStore.TrySetAsComplete(result))
        {
            _logger.ZLogError($"What happened to the existing value?");

            return new();
        }

        _logger.ZLogInformation($"Received value from remote successfully. Result: {result}");

        return new();
    }
}
