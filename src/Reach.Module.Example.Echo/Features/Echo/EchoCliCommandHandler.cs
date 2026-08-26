using Reach.Cli;

namespace Reach.Module.Example.Echo.Features.Echo;

internal sealed class EchoCliCommandHandler
    : CliCommandHandler<EchoCliCommandPayload, EchoCliCommandResult>
{
    private readonly ILogger<EchoCliCommandHandler> _logger;
    private readonly IRpcClient _rpcClient;
    private readonly IConsoleWriter _consoleWriter;

    public EchoCliCommandHandler(
        ILogger<EchoCliCommandHandler> logger,
        IRpcClient rpcClient,
        IConsoleWriter consoleWriter
    )
    {
        _logger = logger;
        _rpcClient = rpcClient;
        _consoleWriter = consoleWriter;
    }

    protected override async Task<EchoCliCommandResult> Handle(
        EchoCliCommandPayload payload,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await _rpcClient.Invoke<EchoPayload, RemoteCommandSubmissionResult>(
                new EchoPayload(payload.Message),
                cancellationToken
            );

            if (result is RemoteCommandSubmissionResult.Failed failed)
            {
                _consoleWriter.WriteLine(failed.Error.Message);

                return new(1);
            }

            if (result is not RemoteCommandSubmissionResult.Accepted accepted)
            {
                throw new NotSupportedException();
            }

            _consoleWriter.WriteLine("Echo sent, waiting for reply...");

            while (true)
            {
                var pollResult = await _rpcClient.Invoke<EchoPollPayload, EchoPollResult>(
                    new EchoPollPayload(accepted.CorrelationId),
                    cancellationToken
                );

                if (pollResult is EchoPollResult.Ongoing)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(payload.PollMs), cancellationToken);

                    continue;
                }

                if (pollResult is EchoPollResult.NotFound)
                {
                    _consoleWriter.WriteLine(
                        $"No echo found for correlation id {accepted.CorrelationId}"
                    );

                    return new(1);
                }

                if (pollResult is EchoPollResult.Error pollError)
                {
                    _consoleWriter.WriteLine($"Echo failed: {pollError.Message}");

                    return new(1);
                }

                if (pollResult is not EchoPollResult.Success pollSuccess)
                {
                    throw new NotSupportedException();
                }

                _consoleWriter.WriteLine($"Received from remote: {pollSuccess.Reply}");

                return new(0);
            }
        }
        catch (Exception ex)
        {
            _consoleWriter.WriteLine($"An error occurred: {ex.Message}");

            _logger.ZLogError(ex, $"An error occurred");

            return new(1);
        }
    }
}
