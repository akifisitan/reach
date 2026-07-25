using System.Collections.Concurrent;

namespace Reach.Plugin.Example.Features.Ping;

internal sealed class PingResultStore
{
    private readonly ConcurrentDictionary<string, PingResultStoreEntry> _store = [];

    public void Insert(string key)
    {
        _store.TryAdd(key, new PingResultStoreEntry { Status = PingStatus.Ongoing });
    }

    public PingResultStoreEntry? GetValue(string key)
    {
        return _store.GetValueOrDefault(key);
    }

    public bool TrySetAsComplete(PingCommandResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var existingValue = _store.GetValueOrDefault(result.CommandId);

        if (existingValue is null)
        {
            return false;
        }

        existingValue.Result = result;
        existingValue.Status = PingStatus.Complete;

        return true;
    }
}

internal sealed record PingResultStoreEntry
{
    public required PingStatus Status { get; set; }
    public PingCommandResult? Result { get; set; }
}

internal enum PingStatus
{
    NotFound = 0,
    Ongoing = 1,
    Complete = 2,
    Failed = 3,
}
