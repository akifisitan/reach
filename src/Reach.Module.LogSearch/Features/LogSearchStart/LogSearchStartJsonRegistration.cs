using Reach.Relay;

namespace Reach.Module.LogSearch.Features.LogSearchStart;

internal sealed partial class LogSearchStartJsonRegistration : IWireJsonRegistration
{
    public IJsonTypeInfoResolver Resolver => LogSearchStartJsonSerializerContext.Default;

    public void Configure(JsonTypeInfo jsonTypeInfo)
    {
        jsonTypeInfo.RegisterDerivedType<RpcPayload, LogSearchStartPayload>("logSearch.start");
        jsonTypeInfo.RegisterWireRecord<RelayCommand, SearchFileCommand>();
        jsonTypeInfo.RegisterWireRecord<RelayCommandResult, SearchFileCommandResult>();
        jsonTypeInfo.RegisterResultTypes<RpcResult, LogSearchStartResult>(
            "logSearch.start.result",
            (typeof(LogSearchStartResult.Success), "logSearch.start.result.success"),
            (typeof(LogSearchStartResult.Error), "logSearch.start.result.error")
        );
    }

    [JsonSerializable(typeof(LogSearchStartPayload))]
    [JsonSerializable(typeof(LogSearchStartResult))]
    [JsonSerializable(typeof(LogSearchStartResult.Success))]
    [JsonSerializable(typeof(LogSearchStartResult.Error))]
    [JsonSerializable(typeof(SearchFileCommand))]
    [JsonSerializable(typeof(SearchFileCommandResult))]
    private sealed partial class LogSearchStartJsonSerializerContext : JsonSerializerContext;
}
