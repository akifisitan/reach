namespace Reach.Module.LogSearch.Features.LogSearchStart;

internal sealed class SearchFileCommandHandler
    : RelayCommandHandler<SearchFileCommand, SearchFileCommandResult>
{
    private readonly FileSearcher _fileSearcher;
    private readonly ILogger<SearchFileCommandHandler> _logger;

    public SearchFileCommandHandler(
        FileSearcher fileSearcher,
        ILogger<SearchFileCommandHandler> logger
    )
    {
        _fileSearcher = fileSearcher;
        _logger = logger;
    }

    protected override async Task<SearchFileCommandResult> Handle(
        SearchFileCommand command,
        CancellationToken cancellationToken
    )
    {
        _logger.ZLogInformation($"Running server execute for {command}");

        var result = await _fileSearcher.Search(command.Query, cancellationToken);

        return new SearchFileCommandResult(command.Id, result.Content);
    }
}
