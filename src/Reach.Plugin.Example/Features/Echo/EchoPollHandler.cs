using Reach.Plugin.Abstractions;

namespace Reach.Plugin.Example.Features.Echo;

internal sealed class EchoPollHandler : RpcHandler<EchoPollPayload, EchoPollResult>
{
    private readonly ILogger<EchoPollHandler> _logger;
    private readonly EchoResultStore _echoResultStore;

    public EchoPollHandler(ILogger<EchoPollHandler> logger, EchoResultStore echoResultStore)
    {
        _logger = logger;
        _echoResultStore = echoResultStore;
    }

    protected override Task<EchoPollResult> Handle(
        EchoPollPayload payload,
        CancellationToken cancellationToken
    )
    {
        var storedValue = _echoResultStore.GetValue(payload.CorrelationId);

        if (storedValue is null)
        {
            return Task.FromResult<EchoPollResult>(new EchoPollResult.NotFound());
        }

        if (storedValue.Result is not null)
        {
            _logger.ZLogInformation($"[{payload.CorrelationId}] Returning received echo");

            return Task.FromResult<EchoPollResult>(
                new EchoPollResult.Success(storedValue.Result.Reply)
            );
        }

        return Task.FromResult<EchoPollResult>(new EchoPollResult.Ongoing());
    }
}
