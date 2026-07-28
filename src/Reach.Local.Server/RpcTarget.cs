using Polly;
using Polly.Registry;
using Reach.Plugin;
using Reach.Relay;

namespace Reach.Local.Server;

internal sealed class RpcTarget
{
    private readonly IHandlerDispatcher<RpcPayload, RpcResult> _handler;
    private readonly IRpcPayloadSerializer _payloadSerializer;
    private readonly ResiliencePipeline _rpcTransportChaos;

    public RpcTarget(
        IHandlerDispatcher<RpcPayload, RpcResult> handler,
        IRpcPayloadSerializer payloadSerializer,
        ResiliencePipelineProvider<string> resiliencePipelineProvider
    )
    {
        _handler = handler;
        _payloadSerializer = payloadSerializer;
        _rpcTransportChaos = resiliencePipelineProvider.GetPipeline(ChaosPolicyNames.RpcTransport);
    }

    public async Task<string> Invoke(string payload, CancellationToken cancellationToken)
    {
        var value = _payloadSerializer.Deserialize<RpcPayload>(payload);

        var result = await _rpcTransportChaos.ExecuteAsync(
            async ct => await _handler.Handle(value, ct),
            cancellationToken
        );

        return _payloadSerializer.Serialize(result);
    }

    public bool HealthCheck() => true;
}
