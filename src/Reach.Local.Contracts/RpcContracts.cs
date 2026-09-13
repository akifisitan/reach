using Nerdbank.JsonRpc;
using Nerdbank.Streams;
using PolyType;

namespace Reach.Local.Contracts;

public sealed class LocalRpcShared
{
    public const string PipeName = "ReachLocalRpcPipe";

    public const string MainMethodName = "main";
    public const string HealthCheckMethodName = "hc";

    public static JsonRpc CreateConnection(Stream stream) =>
        new(new JsonRpcMessagePackChannel(stream.UsePipe()));
}

[GenerateJsonRpcProxy]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial interface ILocalRpc
{
    [MethodShape(Name = LocalRpcShared.MainMethodName)]
    Task<string> Invoke(string payload, CancellationToken cancellationToken);

    [MethodShape(Name = LocalRpcShared.HealthCheckMethodName)]
    Task<bool> HealthCheck(CancellationToken cancellationToken);
}
