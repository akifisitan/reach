using PolyType;
using StreamJsonRpc;

namespace Reach.Local.Shared;

public sealed class LocalRpcShared
{
    public const string PipeName = $"{AppConstants.AppName}LocalRpcPipe";

    public const string MainMethodName = "main";
    public const string HealthCheckMethodName = "hc";

    public static IJsonRpcMessageFormatter CreateFormatter() =>
        new NerdbankMessagePackFormatter { TypeShapeProvider = Witness.GeneratedTypeShapeProvider };
}

[GenerateShapeFor<string>]
internal sealed partial class Witness;
