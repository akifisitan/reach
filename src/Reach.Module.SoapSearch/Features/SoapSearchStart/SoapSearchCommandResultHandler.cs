using Reach.Module.Toolkit.ResultStore;

namespace Reach.Module.SoapSearch.Features.SoapSearchStart;

internal sealed class SoapSearchCommandResultHandler
    : RelayCommandResultHandler<SoapSearchCommandResult>
{
    private readonly IResultStore<SoapSearchCommandResult> _resultStore;

    public SoapSearchCommandResultHandler(IResultStore<SoapSearchCommandResult> resultStore)
    {
        _resultStore = resultStore;
    }

    protected override async Task<Unit> Handle(
        SoapSearchCommandResult result,
        CancellationToken cancellationToken
    )
    {
        await _resultStore.TrySetAsComplete(result, cancellationToken).ConfigureAwait(false);
        return new();
    }
}
