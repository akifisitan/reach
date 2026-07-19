using System.Text.Json.Serialization;

namespace Reach.Relay.Confluence;

public sealed record ResultContentBodyView(string Value, string Representation);

public sealed record ResultContentBody(ResultContentBodyView View);

public sealed record ResultContentHistoryCreatedBy(
    string Type,
    string Username,
    string UserKey,
    string DisplayName
);

public sealed record ResultContentHistory(
    bool Latest,
    ResultContentHistoryCreatedBy CreatedBy,
    DateTimeOffset CreatedDate
);

public sealed record ResultContent(string Id, ResultContentBody Body, ResultContentHistory History);

public sealed record SearchResult(
    ResultContent Content,
    string Excerpt,
    string Title,
    long Timestamp
);

public sealed record SearchResponse(
    List<SearchResult> Results,
    int Start,
    int Limit,
    int Size,
    int TotalSize,
    string CqlQuery,
    int SearchDuration
);

public sealed record GetCommentsResponse(
    List<ResultContent> Results,
    int Start,
    int Limit,
    int Size
)
{
    [JsonPropertyName("_links")]
    public required Links Links { get; init; }
}

public sealed record Links(string Self, string Base, string Context, string? Prev, string? Next);

public sealed record CreateContentRequest(
    string Type,
    CreateContentRequestContainer Container,
    CreateContentRequestBody Body
);

public sealed record CreateContentResponse(
    string Id,
    string Type,
    string Status,
    string Title
// Map other response fields if needed
// Space
// History
// Version
// Container
// Body
);

public sealed record CreateContentRequestContainer(long Id, string Type);

public sealed record CreateContentRequestBody(CreateContentRequestBodyStorage Storage);

public sealed record CreateContentRequestBodyStorage(string Value, string Representation);
