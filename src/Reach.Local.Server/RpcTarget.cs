using Polly;
using Polly.Registry;
using Reach.Hosting;
using Reach.Local.Contracts;
using Reach.Module;

namespace Reach.Local.Server;

internal sealed class RpcTarget : ILocalRpc
{
    private readonly IHandlerDispatcher<RpcPayload, RpcResult> _handlerDispatcher;
    private readonly IRpcPayloadSerializer _payloadSerializer;
    private readonly ResiliencePipeline _chaosPipeline;
    private readonly RemoteCommandCoordinator _remoteCommandCoordinator;

    public RpcTarget(
        IHandlerDispatcher<RpcPayload, RpcResult> handlerDispatcher,
        IRpcPayloadSerializer payloadSerializer,
        ResiliencePipelineProvider<string> resiliencePipelineProvider,
        RemoteCommandCoordinator remoteCommandCoordinator
    )
    {
        _handlerDispatcher = handlerDispatcher;
        _payloadSerializer = payloadSerializer;
        _chaosPipeline = resiliencePipelineProvider.GetPipeline(ChaosPolicyNames.RpcTransport);
        _remoteCommandCoordinator = remoteCommandCoordinator;
    }

    public async Task<string> Invoke(string payload, CancellationToken cancellationToken)
    {
        var value = _payloadSerializer.Deserialize<RpcPayload>(payload);

        var result = await _chaosPipeline
            .ExecuteAsync(
                ct => new ValueTask<RpcResult>(_handlerDispatcher.Handle(value, ct)),
                cancellationToken
            )
            .ConfigureAwait(false);

        if (result is RemoteCommandRequest request)
        {
            result = await _remoteCommandCoordinator
                .Submit(request, cancellationToken)
                .ConfigureAwait(false);
        }

        return _payloadSerializer.Serialize(result);
    }

    public Task<bool> HealthCheck(CancellationToken cancellationToken) => Task.FromResult(true);
}
