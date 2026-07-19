using System.Text.Json.Serialization;

namespace Reach.Relay.Confluence;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    RespectNullableAnnotations = true
)]
[JsonSerializable(typeof(SearchResponse))]
[JsonSerializable(typeof(CreateContentRequest))]
[JsonSerializable(typeof(CreateContentResponse))]
[JsonSerializable(typeof(GetCommentsResponse))]
internal sealed partial class ConfluenceApiJsonSerializerContext : JsonSerializerContext;
