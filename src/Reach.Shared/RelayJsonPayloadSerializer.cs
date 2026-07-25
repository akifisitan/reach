using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Reach.Plugin;
using Reach.Relay;

namespace Reach.Shared;

internal sealed class RelayJsonPayloadSerializer : IRelayPayloadSerializer, IRpcPayloadSerializer
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public RelayJsonPayloadSerializer(JsonSerializerOptions jsonSerializerOptions)
    {
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    public T Deserialize<T>(string payload) =>
        JsonSerializer.Deserialize(
            payload,
            (JsonTypeInfo<T>)_jsonSerializerOptions.GetTypeInfo(typeof(T))
        )
        ?? throw new JsonException(
            $"Deserializing into {typeof(T).FullName ?? typeof(T).Name} resulted in null"
        );

    public string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, _jsonSerializerOptions.GetTypeInfo(typeof(T)));
}
