namespace Reach.Module.Example.LogSearch;

internal sealed class FileSearcher
{
    private readonly ILogger<FileSearcher> _logger;

    public FileSearcher(ILogger<FileSearcher> logger)
    {
        _logger = logger;
    }

    public async Task<SearchFileResult> Search(string query, CancellationToken cancellationToken)
    {
        _logger.ZLogInformation($"Searching in file for {query}...");

        await Task.Delay(Random.Shared.Next(1500, 5000), cancellationToken).ConfigureAwait(false);

        var result = new SearchFileResult(Guid.NewGuid().ToString(), Random.Shared.Next(1, 999999));

        _logger.ZLogInformation($"Search complete: {result}");

        return result;
    }
}
