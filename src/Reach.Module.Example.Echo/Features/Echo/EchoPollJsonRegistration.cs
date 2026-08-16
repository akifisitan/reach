namespace Reach.Module.Example.Echo.Features.Echo;

internal sealed partial class EchoPollJsonRegistration : IWireJsonRegistration
{
    public IJsonTypeInfoResolver Resolver => EchoPollJsonSerializerContext.Default;

    public void Configure(JsonTypeInfo jsonTypeInfo)
    {
        jsonTypeInfo.RegisterDerivedType<RpcPayload, EchoPollPayload>("example.echo.poll");
        jsonTypeInfo.RegisterResultTypes<RpcResult, EchoPollResult>(
            "example.echo.poll.result",
            (typeof(EchoPollResult.NotFound), "example.echo.poll.result.notFound"),
            (typeof(EchoPollResult.Ongoing), "example.echo.poll.result.ongoing"),
            (typeof(EchoPollResult.Success), "example.echo.poll.result.success"),
            (typeof(EchoPollResult.Error), "example.echo.poll.result.error")
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
