namespace Reach.Plugin.LogSearch.Features.LogSearchStatus;

internal sealed partial class LogSearchStatusJsonRegistration : IJsonTypeInfoRegistration
{
    public IJsonTypeInfoResolver Resolver => LogSearchStatusJsonSerializerContext.Default;

    public void Configure(JsonTypeInfo jsonTypeInfo)
    {
        jsonTypeInfo.RegisterDerivedType<RpcPayload, LogSearchStatusPayload>("logSearch.status");
        jsonTypeInfo.RegisterResultTypes<RpcResult, LogSearchStatusResult>(
            "logSearch.status.result",
            (typeof(LogSearchStatusResult.NotFound), "logSearch.status.result.notFound"),
            (typeof(LogSearchStatusResult.Ongoing), "logSearch.status.result.ongoing"),
            (typeof(LogSearchStatusResult.Success), "logSearch.status.result.success"),
            (typeof(LogSearchStatusResult.Expired), "logSearch.status.result.expired"),
            (typeof(LogSearchStatusResult.Error), "logSearch.status.result.error")
        );
    }

    [JsonSerializable(typeof(LogSearchStatusPayload))]
    [JsonSerializable(typeof(LogSearchStatusResult))]
    [JsonSerializable(typeof(LogSearchStatusResult.NotFound))]
    [JsonSerializable(typeof(LogSearchStatusResult.Ongoing))]
    [JsonSerializable(typeof(LogSearchStatusResult.Success))]
    [JsonSerializable(typeof(LogSearchStatusResult.Expired))]
    [JsonSerializable(typeof(LogSearchStatusResult.Error))]
    private sealed partial class LogSearchStatusJsonSerializerContext : JsonSerializerContext;
}
