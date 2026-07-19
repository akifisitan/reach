using System.Collections.Concurrent;
using Reach.Relay.Abstractions;

namespace Reach.Remote.Agent;

internal sealed class RelayCommandStore
{
    private readonly ConcurrentDictionary<string, RelayCommand> _store = [];

    public bool Contains(string id)
    {
        return _store.ContainsKey(id);
    }

    public void Add(RelayCommand command)
    {
        _store.TryAdd(command.Id, command);
    }
}
