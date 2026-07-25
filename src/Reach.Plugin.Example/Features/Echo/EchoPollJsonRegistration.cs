namespace Reach.Plugin.Example.Features.Echo;

internal sealed partial class EchoPollJsonRegistration : IJsonTypeInfoRegistration
{
    public IJsonTypeInfoResolver Resolver => EchoPollJsonSerializerContext.Default;

    public void Configure(JsonTypeInfo jsonTypeInfo)
    {
        jsonTypeInfo.RegisterDerivedType<RpcPayload, EchoPollPayload>("example.echoPoll");
        jsonTypeInfo.RegisterResultTypes<RpcResult, EchoPollResult>(
            "example.echoPoll.result",
            (typeof(EchoPollResult.NotFound), "example.echoPoll.result.notFound"),
            (typeof(EchoPollResult.Ongoing), "example.echoPoll.result.ongoing"),
            (typeof(EchoPollResult.Success), "example.echoPoll.result.success"),
            (typeof(EchoPollResult.Error), "example.echoPoll.result.error")
        );
    }

    [JsonSerializable(typeof(EchoPollPayload))]
    [JsonSerializable(typeof(EchoPollResult))]
    [JsonSerializable(typeof(EchoPollResult.NotFound))]
    [JsonSerializable(typeof(EchoPollResult.Ongoing))]
    [JsonSerializable(typeof(EchoPollResult.Success))]
    [JsonSerializable(typeof(EchoPollResult.Error))]
    private sealed partial class EchoPollJsonSerializerContext : JsonSerializerContext;
}
