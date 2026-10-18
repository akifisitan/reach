using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Reach.Relay;

namespace Reach.Module.SoapSearch.Features.SoapSearchStart;

internal sealed partial class SoapSearchStartJsonRegistration : IWireJsonRegistration
{
    public IJsonTypeInfoResolver Resolver => SoapSearchStartJsonContext.Default;

    public void Configure(JsonTypeInfo jsonTypeInfo)
    {
        jsonTypeInfo.RegisterDerivedType<RpcPayload, SoapSearchStartPayload>("soapSearch.start");
        jsonTypeInfo.RegisterWireRecord<RelayCommand, SoapSearchCommand>();
        jsonTypeInfo.RegisterWireRecord<RelayCommandResult, SoapSearchCommandResult>();
    }

    [JsonSerializable(typeof(SoapSearchStartPayload))]
    [JsonSerializable(typeof(SoapSearchCommand))]
    [JsonSerializable(typeof(SoapSearchCommandResult))]
    private sealed partial class SoapSearchStartJsonContext : JsonSerializerContext;
}
