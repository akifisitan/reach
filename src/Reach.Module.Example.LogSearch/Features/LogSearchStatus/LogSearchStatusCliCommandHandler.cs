using Reach.Cli;

namespace Reach.Module.Example.LogSearch.Features.LogSearchStatus;

internal sealed class LogSearchStatusCliCommandHandler
    : CliCommandHandler<LogSearchStatusCliCommandPayload, LogSearchStatusCliCommandResult>
{
    private readonly ILogger<LogSearchStatusCliCommandHandler> _logger;
    private readonly IRpcClient _localClient;
    private readonly IConsoleWriter _consoleWriter;

    public LogSearchStatusCliCommandHandler(
        ILogger<LogSearchStatusCliCommandHandler> logger,
        IRpcClient localClient,
        IConsoleWriter consoleWriter
    )
    {
        _logger = logger;
        _localClient = localClient;
        _consoleWriter = consoleWriter;
    }

    protected override async Task<LogSearchStatusCliCommandResult> Handle(
        LogSearchStatusCliCommandPayload cliPayload,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var payload = new LogSearchStatusPayload(cliPayload.CorrelationId);

            var result = await _localClient
                .Invoke<LogSearchStatusPayload, LogSearchStatusResult>(payload, cancellationToken)
                .ConfigureAwait(false);

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
