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
    }

    [JsonSerializable(typeof(LogSearchStartPayload))]
    [JsonSerializable(typeof(SearchFileCommand))]
    [JsonSerializable(typeof(SearchFileCommandResult))]
    private sealed partial class LogSearchStartJsonSerializerContext : JsonSerializerContext;
}
