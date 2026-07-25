using Reach.Plugin.Abstractions;

namespace Reach.Plugin.Example.Features.Ping;

internal sealed partial class PingPollJsonRegistration : IJsonTypeInfoRegistration
{
    public IJsonTypeInfoResolver Resolver => PingPollJsonSerializerContext.Default;

    public void Configure(JsonTypeInfo jsonTypeInfo)
    {
        jsonTypeInfo.RegisterDerivedType<RpcPayload, PingPollPayload>("example.pingPoll");
        jsonTypeInfo.RegisterResultTypes<RpcResult, PingPollResult>(
            "example.pingPoll.result",
            (typeof(PingPollResult.NotFound), "example.pingPoll.result.notFound"),
            (typeof(PingPollResult.Ongoing), "example.pingPoll.result.ongoing"),
            (typeof(PingPollResult.Success), "example.pingPoll.result.success")
        );
    }

    [JsonSerializable(typeof(PingPollPayload))]
    [JsonSerializable(typeof(PingPollResult))]
    [JsonSerializable(typeof(PingPollResult.NotFound))]
    [JsonSerializable(typeof(PingPollResult.Ongoing))]
    [JsonSerializable(typeof(PingPollResult.Success))]
    private sealed partial class PingPollJsonSerializerContext : JsonSerializerContext;
}
