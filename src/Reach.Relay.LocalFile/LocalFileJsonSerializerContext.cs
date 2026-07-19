using System.Text.Json.Serialization;

namespace Reach.Relay.LocalFile;

[JsonSourceGenerationOptions(RespectNullableAnnotations = true)]
[JsonSerializable(typeof(Storage))]
internal sealed partial class LocalFileJsonSerializerContext : JsonSerializerContext;
