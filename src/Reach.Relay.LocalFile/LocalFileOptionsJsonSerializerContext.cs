using System.Text.Json.Serialization;

namespace Reach.Relay.LocalFile;

[JsonSourceGenerationOptions(
    RespectNullableAnnotations = true,
    WriteIndented = true,
    IndentCharacter = ' ',
    IndentSize = 2
)]
[JsonSerializable(typeof(LocalFileRelayOptions))]
internal sealed partial class LocalFileOptionsJsonSerializerContext : JsonSerializerContext;
