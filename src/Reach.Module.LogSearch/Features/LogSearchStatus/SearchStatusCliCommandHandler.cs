namespace Reach.Module.LogSearch.Features.LogSearchStatus;

internal sealed class SearchStatusCliCommandHandler
    : CliCommandHandler<SearchStatusCliCommandPayload, SearchStatusCliCommandResult>
{
    private readonly ILogger<SearchStatusCliCommandHandler> _logger;
    private readonly IRpcClient _localClient;
    private readonly IConsoleWriter _consoleWriter;

    public SearchStatusCliCommandHandler(
        ILogger<SearchStatusCliCommandHandler> logger,
        IRpcClient localClient,
        IConsoleWriter consoleWriter
    )
    {
        _logger = logger;
        _localClient = localClient;
        _consoleWriter = consoleWriter;
    }

    protected override async Task<SearchStatusCliCommandResult> Handle(
        SearchStatusCliCommandPayload cliPayload,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var payload = new LogSearchStatusPayload(cliPayload.CorrelationId);

            var result = await _localClient.Invoke<LogSearchStatusPayload, LogSearchStatusResult>(
                payload,
                cancellationToken
            );

            _consoleWriter.WriteLine($"Result: {result}");

            return new(0);
        }
        catch (Exception ex)
        {
            _consoleWriter.WriteLine($"An error occurred: {ex.Message}");

            _logger.ZLogError(ex, $"An error occurred");

            return new(1);
        }
    }
}
