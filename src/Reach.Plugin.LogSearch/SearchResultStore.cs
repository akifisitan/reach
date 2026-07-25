using System.Collections.Concurrent;
using Reach.Plugin.LogSearch.Features.LogSearchStart;

namespace Reach.Plugin.LogSearch;

// Make it durable?
internal sealed class SearchResultStore
{
    private readonly ConcurrentDictionary<string, SearchResultStoreEntry> _store = [];

    public void Insert(string key)
    {
        _store.TryAdd(key, new SearchResultStoreEntry { Status = SearchStatus.Ongoing });
    }

    public SearchResultStoreEntry? GetValue(string key)
    {
        return _store.GetValueOrDefault(key);
    }

    public bool TrySetAsComplete(SearchFileCommandResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var existingValue = _store.GetValueOrDefault(result.CommandId);

        if (existingValue is null)
        {
            return false;
        }

        existingValue.Result = result;
        existingValue.Status = SearchStatus.Complete;

        return true;
    }
}

internal sealed record SearchResultStoreEntry
{
    public required SearchStatus Status { get; set; }
    public SearchFileCommandResult? Result { get; set; }
}

internal enum SearchStatus
{
    NotFound = 0,
    Ongoing = 1,
    Complete = 2,
    Failed = 3,
}
