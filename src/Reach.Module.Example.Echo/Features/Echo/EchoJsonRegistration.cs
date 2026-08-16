using Reach.Relay;

namespace Reach.Module.Example.Echo.Features.Echo;

internal sealed partial class EchoJsonRegistration : IWireJsonRegistration
{
    public IJsonTypeInfoResolver Resolver => EchoJsonSerializerContext.Default;

    public void Configure(JsonTypeInfo jsonTypeInfo)
    {
        jsonTypeInfo.RegisterDerivedType<RpcPayload, EchoPayload>("example.echo.start");
        jsonTypeInfo.RegisterWireRecord<RelayCommand, EchoCommand>();
        jsonTypeInfo.RegisterWireRecord<RelayCommandResult, EchoCommandResult>();
    }

    [JsonSerializable(typeof(EchoPayload))]
    [JsonSerializable(typeof(EchoCommand))]
    [JsonSerializable(typeof(EchoCommandResult))]
    private sealed partial class EchoJsonSerializerContext : JsonSerializerContext;
}
