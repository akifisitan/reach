using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Reach.Infra.DurableQueue;
using Reach.Module;
using Reach.Relay;

namespace Reach.Hosting;

internal sealed class JsonPayloadSerializer
    : IRelayPayloadSerializer,
        IRpcPayloadSerializer,
        IDurableQueueDataSerializer
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public JsonPayloadSerializer(JsonSerializerOptions jsonSerializerOptions)
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
