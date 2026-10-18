using Reach.Module.SoapSearch.Features.SoapSearchStart;
using Reach.Module.Toolkit.ResultStore;

namespace Reach.Module.SoapSearch.Features.SoapSearchStatus;

internal sealed class SoapSearchStatusHandler
    : RpcHandler<SoapSearchStatusPayload, SoapSearchStatusResult>
{
    private readonly IResultStore<SoapSearchCommandResult> _resultStore;

    public SoapSearchStatusHandler(IResultStore<SoapSearchCommandResult> resultStore)
    {
        _resultStore = resultStore;
    }

    protected override async Task<SoapSearchStatusResult> Handle(
        SoapSearchStatusPayload payload,
        CancellationToken cancellationToken
    )
    {
        var value = await _resultStore
            .GetValue(payload.CorrelationId, cancellationToken)
            .ConfigureAwait(false);
        if (value is null)
        {
            return new SoapSearchStatusResult.NotFound();
        }

        if (value.Result is not null)
        {
            return new SoapSearchStatusResult.Success(value.Result.Value);
        }

        if (value.Status == ResultStoreStatus.Failed)
        {
            return new SoapSearchStatusResult.Error(value.ErrorMessage ?? "Search failed");
        }

        return DateTimeOffset.Now - value.CreationTime > _resultStore.StalenessThreshold
            ? new SoapSearchStatusResult.Expired()
            : new SoapSearchStatusResult.Ongoing();
    }
}
