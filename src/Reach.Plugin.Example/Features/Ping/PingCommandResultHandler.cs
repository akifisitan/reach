using Reach.Plugin.Abstractions;

namespace Reach.Plugin.Example.Features.Ping;

internal sealed class PingCommandResultHandler : RelayCommandResultHandler<PingCommandResult>
{
    private readonly PingResultStore _pingResultStore;

    public PingCommandResultHandler(PingResultStore pingResultStore)
    {
        _pingResultStore = pingResultStore;
    }

    protected override Task<Unit> Handle(
        PingCommandResult result,
        CancellationToken cancellationToken
    )
    {
        _pingResultStore.TrySetAsComplete(result);

        return Task.FromResult(new Unit());
    }
}
