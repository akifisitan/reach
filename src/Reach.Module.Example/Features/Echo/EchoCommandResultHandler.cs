using Reach.Module.Toolkit.ResultStore;

namespace Reach.Module.Example.Features.Echo;

internal sealed class EchoCommandResultHandler : RelayCommandResultHandler<EchoCommandResult>
{
    private readonly IResultStore<EchoCommandResult> _resultStore;

    public EchoCommandResultHandler(IResultStore<EchoCommandResult> resultStore)
    {
        _resultStore = resultStore;
    }

    protected override async Task<Unit> Handle(
        EchoCommandResult result,
        CancellationToken cancellationToken
    )
    {
        await _resultStore.TrySetAsComplete(result, cancellationToken);

        return new();
    }
}
