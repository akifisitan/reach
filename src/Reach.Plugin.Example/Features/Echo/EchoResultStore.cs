using System.Collections.Concurrent;

namespace Reach.Plugin.Example.Features.Echo;

internal sealed class EchoResultStore
{
    private readonly ConcurrentDictionary<string, EchoResultStoreEntry> _store = [];

    public void Insert(string key)
    {
        _store.TryAdd(key, new EchoResultStoreEntry { Status = EchoStatus.Ongoing });
    }

    public EchoResultStoreEntry? GetValue(string key)
    {
        return _store.GetValueOrDefault(key);
    }

    public bool TrySetAsComplete(EchoCommandResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var existingValue = _store.GetValueOrDefault(result.CommandId);

        if (existingValue is null)
        {
            return false;
        }

        existingValue.Result = result;
        existingValue.Status = EchoStatus.Complete;

        return true;
    }
}

internal sealed record EchoResultStoreEntry
{
    public required EchoStatus Status { get; set; }
    public EchoCommandResult? Result { get; set; }
}

internal enum EchoStatus
{
    NotFound = 0,
    Ongoing = 1,
    Complete = 2,
    Failed = 3,
}
