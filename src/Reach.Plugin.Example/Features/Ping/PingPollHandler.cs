using Reach.Plugin.Abstractions;

namespace Reach.Plugin.Example.Features.Ping;

internal sealed class PingPollHandler : RpcHandler<PingPollPayload, PingPollResult>
{
    private readonly ILogger<PingPollHandler> _logger;
    private readonly PingResultStore _pingResultStore;

    public PingPollHandler(ILogger<PingPollHandler> logger, PingResultStore pingResultStore)
    {
        _logger = logger;
        _pingResultStore = pingResultStore;
    }

    protected override Task<PingPollResult> Handle(
        PingPollPayload payload,
        CancellationToken cancellationToken
    )
    {
        var storedValue = _pingResultStore.GetValue(payload.CorrelationId);

        if (storedValue is null)
        {
            return Task.FromResult<PingPollResult>(new PingPollResult.NotFound());
        }

        if (storedValue.Result is not null)
        {
            _logger.ZLogInformation($"[{payload.CorrelationId}] Returning received pong");

            return Task.FromResult<PingPollResult>(
                new PingPollResult.Success(storedValue.Result.Reply)
            );
        }

        return Task.FromResult<PingPollResult>(new PingPollResult.Ongoing());
    }
}
