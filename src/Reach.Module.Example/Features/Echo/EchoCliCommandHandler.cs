namespace Reach.Module.Example.Features.Echo;

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
            var result = await _rpcClient.Invoke<EchoPayload, EchoResult>(
                new EchoPayload(payload.Message),
                cancellationToken
            );

            switch (result)
            {
                case EchoResult.Success success:
                {
                    _consoleWriter.WriteLine("Echo sent, waiting for reply...");

                    while (true)
                    {
                        var pollResult = await _rpcClient.Invoke<EchoPollPayload, EchoPollResult>(
                            new EchoPollPayload(success.CorrelationId),
                            cancellationToken
                        );

                        switch (pollResult)
                        {
                            case EchoPollResult.Success pollSuccess:
                            {
                                _consoleWriter.WriteLine(
                                    $"Received from remote: {pollSuccess.Reply}"
                                );

                                return new(0);
                            }
                            case EchoPollResult.Ongoing:
                            {
                                await Task.Delay(
                                    TimeSpan.FromMilliseconds(payload.PollMs),
                                    cancellationToken
                                );

                                break;
                            }
                            case EchoPollResult.NotFound:
                            {
                                _consoleWriter.WriteLine(
                                    $"No echo found for correlation id {success.CorrelationId}"
                                );

                                return new(1);
                            }
                            case EchoPollResult.Error pollError:
                            {
                                _consoleWriter.WriteLine($"Echo failed: {pollError.Message}");

                                return new(1);
                            }
                            default:
                                throw new NotSupportedException();
                        }
                    }
                }
                case EchoResult.Error err:
                {
                    _consoleWriter.WriteLine(err.Message);

                    return new(1);
                }
                default:
                    throw new NotSupportedException();
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
