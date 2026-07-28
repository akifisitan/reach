using Reach.Plugin.Toolkit.ResultStore;

namespace Reach.Plugin.LogSearch.Features.LogSearchStart;

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
        _logger.ZLogInformation($"Doing something with command: {result.Id}");

        if (!await _resultStore.TrySetAsComplete(result, cancellationToken))
        {
            _logger.ZLogError($"What happened to the existing value?");

            return new();
        }

        _logger.ZLogInformation($"Received value from remote successfully. Result: {result}");

        return new();
    }
}
