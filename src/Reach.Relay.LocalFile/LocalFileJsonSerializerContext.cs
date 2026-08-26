using System.Text.Json.Serialization;

namespace Reach.Relay.LocalFile;

[JsonSourceGenerationOptions(RespectNullableAnnotations = true)]
[JsonSerializable(typeof(Storage))]
[JsonSerializable(typeof(LocalFileRelayOptions))]
internal sealed partial class LocalFileJsonSerializerContext : JsonSerializerContext;
