using Reach.Cli;

namespace Reach.Module.Example.LogSearch.Features.LogSearchStart;

internal sealed class LogSearchStartCliCommandHandler
    : CliCommandHandler<LogSearchStartCliCommandPayload, LogSearchStartCliCommandResult>
{
    private readonly ILogger<LogSearchStartCliCommandHandler> _logger;
    private readonly IRpcClient _rpcClient;
    private readonly IConsoleWriter _consoleWriter;

    public LogSearchStartCliCommandHandler(
        ILogger<LogSearchStartCliCommandHandler> logger,
        IRpcClient rpcClient,
        IConsoleWriter consoleWriter
    )
    {
        _logger = logger;
        _rpcClient = rpcClient;
        _consoleWriter = consoleWriter;
    }

    protected override async Task<LogSearchStartCliCommandResult> Handle(
        LogSearchStartCliCommandPayload payload,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var startPayload = new LogSearchStartPayload(
                payload.Query,
                payload.ServerName,
                payload.StartTime,
                payload.EndTime
            );

            var result = await _rpcClient
                .Invoke<LogSearchStartPayload, RemoteCommandSubmissionResult>(
                    startPayload,
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (result is RemoteCommandSubmissionResult.Failed failed)
            {
                _consoleWriter.WriteLine(failed.Error.Message);

                return new(1);
            }

            if (result is not RemoteCommandSubmissionResult.Accepted accepted)
            {
                throw new NotSupportedException();
            }

            _consoleWriter.WriteLine($"Can poll with {accepted.CorrelationId}");

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
