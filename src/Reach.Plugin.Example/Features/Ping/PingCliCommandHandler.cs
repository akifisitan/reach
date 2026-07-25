using Reach.Plugin.Abstractions;

namespace Reach.Plugin.Example.Features.Ping;

internal sealed class PingCliCommandHandler
    : CliCommandHandler<PingCliCommandPayload, PingCliCommandResult>
{
    private readonly ILogger<PingCliCommandHandler> _logger;
    private readonly IRpcClient _rpcClient;

    public PingCliCommandHandler(ILogger<PingCliCommandHandler> logger, IRpcClient rpcClient)
    {
        _logger = logger;
        _rpcClient = rpcClient;
    }

    protected override async Task<PingCliCommandResult> Handle(
        PingCliCommandPayload payload,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var pingPayload = new PingPayload(Guid.NewGuid().ToString("N"), payload.Message);

            var result = await _rpcClient.Invoke<PingPayload, PingResult>(
                pingPayload,
                cancellationToken
            );

            switch (result)
            {
                case PingResult.Success success:
                {
                    Console.WriteLine("Ping sent, waiting for pong...");

                    while (true)
                    {
                        var pollResult = await _rpcClient.Invoke<PingPollPayload, PingPollResult>(
                            new PingPollPayload(success.CorrelationId),
                            cancellationToken
                        );

                        switch (pollResult)
                        {
                            case PingPollResult.Success pollSuccess:
                            {
                                Console.WriteLine($"Received from remote: {pollSuccess.Reply}");

                                return new(0);
                            }
                            case PingPollResult.Ongoing:
                            {
                                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

                                break;
                            }
                            case PingPollResult.NotFound:
                            {
                                Console.WriteLine(
                                    $"No ping found for correlation id {success.CorrelationId}"
                                );

                                return new(1);
                            }
                            default:
                                throw new NotSupportedException();
                        }
                    }
                }
                case PingResult.Error err:
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
