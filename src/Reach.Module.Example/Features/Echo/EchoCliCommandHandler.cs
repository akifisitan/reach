namespace Reach.Module.Example.Features.Echo;

internal sealed class EchoCliCommandHandler
    : CliCommandHandler<EchoCliCommandPayload, EchoCliCommandResult>
{
    private readonly ILogger<EchoCliCommandHandler> _logger;
    private readonly IRpcClient _rpcClient;

    public EchoCliCommandHandler(ILogger<EchoCliCommandHandler> logger, IRpcClient rpcClient)
    {
        _logger = logger;
        _rpcClient = rpcClient;
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
                    Console.WriteLine("Echo sent, waiting for reply...");

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
                                Console.WriteLine($"Received from remote: {pollSuccess.Reply}");

                                return new(0);
                            }
                            case EchoPollResult.Ongoing:
                            {
                                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

                                break;
                            }
                            case EchoPollResult.NotFound:
                            {
                                Console.WriteLine(
                                    $"No echo found for correlation id {success.CorrelationId}"
                                );

                                return new(1);
                            }
                            case EchoPollResult.Error pollError:
                            {
                                Console.WriteLine($"Echo failed: {pollError.Message}");

                                return new(1);
                            }
                            default:
                                throw new NotSupportedException();
                        }
                    }
                }
                case EchoResult.Error err:
                {
                    Console.WriteLine(err.Message);

                    return new(1);
                }
                default:
                    throw new NotSupportedException();
            }
        }
        catch (OperationCanceledException)
        {
            return new(1);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"An error occurred");

            return new(1);
        }
    }
}
