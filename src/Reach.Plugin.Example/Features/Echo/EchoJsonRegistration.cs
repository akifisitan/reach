using Reach.Plugin.Abstractions;
using Reach.Relay.Abstractions;

namespace Reach.Plugin.Example.Features.Echo;

internal sealed partial class EchoJsonRegistration : IJsonTypeInfoRegistration
{
    public IJsonTypeInfoResolver Resolver => EchoJsonSerializerContext.Default;

    public void Configure(JsonTypeInfo jsonTypeInfo)
    {
        jsonTypeInfo.RegisterDerivedType<RpcPayload, EchoPayload>("example.echo");
        jsonTypeInfo.RegisterWireRecord<RelayCommand, EchoCommand>();
        jsonTypeInfo.RegisterWireRecord<RelayCommandResult, EchoCommandResult>();
        jsonTypeInfo.RegisterResultTypes<RpcResult, EchoResult>(
            "example.echo.result",
            (typeof(EchoResult.Success), "example.echo.result.success"),
            (typeof(EchoResult.Error), "example.echo.result.error")
        );
    }

    [JsonSerializable(typeof(EchoPayload))]
    [JsonSerializable(typeof(EchoResult))]
    [JsonSerializable(typeof(EchoResult.Success))]
    [JsonSerializable(typeof(EchoResult.Error))]
    [JsonSerializable(typeof(EchoCommand))]
    [JsonSerializable(typeof(EchoCommandResult))]
    private sealed partial class EchoJsonSerializerContext : JsonSerializerContext;
}
