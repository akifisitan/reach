using Reach.Plugin.Abstractions;

namespace Reach.Plugin.Example.Features.Echo;

internal sealed class EchoCommandResultHandler : RelayCommandResultHandler<EchoCommandResult>
{
    private readonly EchoResultStore _echoResultStore;

    public EchoCommandResultHandler(EchoResultStore echoResultStore)
    {
        _echoResultStore = echoResultStore;
    }

    protected override Task<Unit> Handle(
        EchoCommandResult result,
        CancellationToken cancellationToken
    )
    {
        _echoResultStore.TrySetAsComplete(result);

        return Task.FromResult(new Unit());
    }
}
