using Reach.Plugin;

namespace Reach.Local.Server;

internal sealed class RpcTarget
{
    private readonly IHandlerDispatcher<RpcPayload, RpcResult> _handler;
    private readonly IRpcPayloadSerializer _payloadSerializer;

    public RpcTarget(
        IHandlerDispatcher<RpcPayload, RpcResult> handler,
        IRpcPayloadSerializer payloadSerializer
    )
    {
        _handler = handler;
        _payloadSerializer = payloadSerializer;
    }

    public async Task<string> Invoke(string payload, CancellationToken cancellationToken)
    {
        var value = _payloadSerializer.Deserialize<RpcPayload>(payload);

        var result = await _handler.Handle(value, cancellationToken);

        return _payloadSerializer.Serialize(result);
    }

    public bool HealthCheck() => true;
}
