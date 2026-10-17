namespace Reach.Module.Example.LogSearch.Features.LogSearchStart;

internal sealed class SearchFileCommandHandler
    : RelayCommandHandler<SearchFileCommand, SearchFileCommandResult>
{
    private readonly IFileSearcher _fileSearcher;
    private readonly ILogger<SearchFileCommandHandler> _logger;

    public SearchFileCommandHandler(
        IFileSearcher fileSearcher,
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

        var result = await _fileSearcher
            .Search(command.Query, cancellationToken)
            .ConfigureAwait(false);

        return new SearchFileCommandResult(command.Id, result.Content, result.LineNumber);
    }
}
