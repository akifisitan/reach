using System.Text.Json.Serialization;

namespace Reach.Relay.Confluence;

[JsonSourceGenerationOptions(
    RespectNullableAnnotations = true,
    WriteIndented = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    IndentCharacter = ' ',
    IndentSize = 2
)]
[JsonSerializable(typeof(ConfluenceOptions))]
internal sealed partial class ConfluenceJsonSerializerContext : JsonSerializerContext;
