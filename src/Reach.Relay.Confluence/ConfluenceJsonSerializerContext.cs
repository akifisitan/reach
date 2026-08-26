using System.Text.Json.Serialization;

namespace Reach.Relay.Confluence;

[JsonSourceGenerationOptions(
    RespectNullableAnnotations = true,
    WriteIndented = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
)]
[JsonSerializable(typeof(ConfluenceOptions))]
internal sealed partial class ConfluenceJsonSerializerContext : JsonSerializerContext;
