namespace Reach.Plugin.LogSearch;

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

        await Task.Delay(2000, cancellationToken);

        _logger.ZLogInformation($"Search complete");

        return new(Guid.NewGuid().ToString(), 4321);
    }
}
