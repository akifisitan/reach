using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Reach.Module.SoapSearch.Features.SoapSearchStatus;

internal sealed partial class SoapSearchStatusJsonRegistration : IWireJsonRegistration
{
    public IJsonTypeInfoResolver Resolver => SoapSearchStatusJsonContext.Default;

    public void Configure(JsonTypeInfo jsonTypeInfo)
    {
        jsonTypeInfo.RegisterDerivedType<RpcPayload, SoapSearchStatusPayload>("soapSearch.status");
        jsonTypeInfo.RegisterResultTypes<RpcResult, SoapSearchStatusResult>(
            "soapSearch.status.result",
            (typeof(SoapSearchStatusResult.NotFound), "soapSearch.status.result.notFound"),
            (typeof(SoapSearchStatusResult.Ongoing), "soapSearch.status.result.ongoing"),
            (typeof(SoapSearchStatusResult.Success), "soapSearch.status.result.success"),
            (typeof(SoapSearchStatusResult.Expired), "soapSearch.status.result.expired"),
            (typeof(SoapSearchStatusResult.Error), "soapSearch.status.result.error")
        );
    }

    [JsonSerializable(typeof(SoapSearchStatusPayload))]
    [JsonSerializable(typeof(SoapSearchStatusResult))]
    [JsonSerializable(typeof(SoapSearchStatusResult.NotFound))]
    [JsonSerializable(typeof(SoapSearchStatusResult.Ongoing))]
    [JsonSerializable(typeof(SoapSearchStatusResult.Success))]
    [JsonSerializable(typeof(SoapSearchStatusResult.Expired))]
    [JsonSerializable(typeof(SoapSearchStatusResult.Error))]
    private sealed partial class SoapSearchStatusJsonContext : JsonSerializerContext;
}
