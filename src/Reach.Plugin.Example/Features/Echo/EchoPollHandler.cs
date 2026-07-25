using Reach.Plugin.Abstractions;
using Reach.Plugin.Toolkit.ResultStore.Abstractions;

namespace Reach.Plugin.Example.Features.Echo;

internal sealed class EchoPollHandler : RpcHandler<EchoPollPayload, EchoPollResult>
{
    private readonly ILogger<EchoPollHandler> _logger;
    private readonly IResultStore<EchoCommandResult> _resultStore;

    public EchoPollHandler(
        ILogger<EchoPollHandler> logger,
        IResultStore<EchoCommandResult> resultStore
    )
    {
        _logger = logger;
        _resultStore = resultStore;
    }

    protected override async Task<EchoPollResult> Handle(
        EchoPollPayload payload,
        CancellationToken cancellationToken
    )
    {
        var storedValue = await _resultStore.GetValue(payload.CorrelationId, cancellationToken);

        if (storedValue is null)
        {
            return new EchoPollResult.NotFound();
        }

        if (storedValue.Result is not null)
        {
            _logger.ZLogInformation($"[{payload.CorrelationId}] Returning received echo");

            return new EchoPollResult.Success(storedValue.Result.Reply);
        }

        if (storedValue.Status is ResultStoreStatus.Failed)
        {
            return new EchoPollResult.Error(storedValue.ErrorMessage ?? "Echo failed");
        }

        return new EchoPollResult.Ongoing();
    }
}
