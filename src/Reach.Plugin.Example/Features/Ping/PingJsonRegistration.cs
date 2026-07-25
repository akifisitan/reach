using Reach.Plugin.Abstractions;
using Reach.Relay.Abstractions;

namespace Reach.Plugin.Example.Features.Ping;

internal sealed partial class PingJsonRegistration : IJsonTypeInfoRegistration
{
    public IJsonTypeInfoResolver Resolver => PingJsonSerializerContext.Default;

    public void Configure(JsonTypeInfo jsonTypeInfo)
    {
        jsonTypeInfo.RegisterDerivedType<RpcPayload, PingPayload>("example.ping");
        jsonTypeInfo.RegisterWireRecord<RelayCommand, PingCommand>();
        jsonTypeInfo.RegisterWireRecord<RelayCommandResult, PingCommandResult>();
        jsonTypeInfo.RegisterResultTypes<RpcResult, PingResult>(
            "example.ping.result",
            (typeof(PingResult.Success), "example.ping.result.success"),
            (typeof(PingResult.Error), "example.ping.result.error")
        );
    }

    [JsonSerializable(typeof(PingPayload))]
    [JsonSerializable(typeof(PingResult))]
    [JsonSerializable(typeof(PingResult.Success))]
    [JsonSerializable(typeof(PingResult.Error))]
    [JsonSerializable(typeof(PingCommand))]
    [JsonSerializable(typeof(PingCommandResult))]
    private sealed partial class PingJsonSerializerContext : JsonSerializerContext;
}
