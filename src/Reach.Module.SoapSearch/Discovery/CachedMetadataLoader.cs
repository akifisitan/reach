using System.Text.Json;
using System.Text.Json.Serialization;

namespace Reach.Module.SoapSearch.Discovery;

// Serara's cached-metadata.json schema. Keep names compatible with existing caches.
internal sealed record WinApplicationMetadata
{
    public required string Name { get; init; }
    public required string TeamName { get; init; }
    public required string UK { get; init; }
    public required string ACI { get; init; }
    public required string LogPath { get; init; }
    public required string SearchTerm { get; init; }
    public required List<string> Servers { get; init; }
    public required string LogSearchPattern { get; init; }
    public required string ArchiveDirectoryName { get; init; }
}

internal static class CachedMetadataLoader
{
    // Copied from Serara's JsonFileStore.ReadAsync / CachedMetadataLoader.Load.
    // Propagate read errors so the caller can report an incomplete search.
    public static async Task<List<WinApplicationMetadata>> Load(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        var content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize(
                content,
                MetadataJsonContext.Default.ListWinApplicationMetadata
            ) ?? throw new JsonException("The metadata file must contain an application array.");
    }
}

[JsonSourceGenerationOptions(
    RespectNullableAnnotations = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
)]
[JsonSerializable(typeof(List<WinApplicationMetadata>))]
internal sealed partial class MetadataJsonContext : JsonSerializerContext;
