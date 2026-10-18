using Reach.Module.Toolkit.ResultStore;

namespace Reach.Module.SoapSearch.Features.SoapSearchStart;

internal sealed class SoapSearchStartHandler
    : RemoteCommandRpcHandler<SoapSearchStartPayload, SoapSearchCommand>
{
    private readonly IResultStore<SoapSearchCommandResult> _resultStore;

    public SoapSearchStartHandler(IResultStore<SoapSearchCommandResult> resultStore)
    {
        _resultStore = resultStore;
    }

    protected override async Task<SoapSearchCommand> PrepareCommand(
        SoapSearchStartPayload payload,
        CancellationToken cancellationToken
    )
    {
        payload.Request.Validate();
        var id = Guid.CreateVersion7().ToString("N");
        await _resultStore.Insert(id, cancellationToken).ConfigureAwait(false);
        return new(id, payload.Request);
    }

    protected override Task HandleSubmissionFailure(
        SoapSearchCommand command,
        Exception exception,
        CancellationToken cancellationToken
    ) => _resultStore.TrySetAsFailed(command.Id, exception.Message, cancellationToken);
}
