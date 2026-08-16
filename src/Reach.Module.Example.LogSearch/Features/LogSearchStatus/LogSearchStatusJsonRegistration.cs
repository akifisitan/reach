namespace Reach.Module.Example.LogSearch.Features.LogSearchStatus;

internal sealed partial class LogSearchStatusJsonRegistration : IWireJsonRegistration
{
    public IJsonTypeInfoResolver Resolver => LogSearchStatusJsonSerializerContext.Default;

    public void Configure(JsonTypeInfo jsonTypeInfo)
    {
        jsonTypeInfo.RegisterDerivedType<RpcPayload, LogSearchStatusPayload>(
            "example.logSearch.status"
        );
        jsonTypeInfo.RegisterResultTypes<RpcResult, LogSearchStatusResult>(
            "example.logSearch.status.result",
            (typeof(LogSearchStatusResult.NotFound), "example.logSearch.status.result.notFound"),
            (typeof(LogSearchStatusResult.Ongoing), "example.logSearch.status.result.ongoing"),
            (typeof(LogSearchStatusResult.Success), "example.logSearch.status.result.success"),
            (typeof(LogSearchStatusResult.Expired), "example.logSearch.status.result.expired"),
            (typeof(LogSearchStatusResult.Error), "example.logSearch.status.result.error")
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
