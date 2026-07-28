using System.Collections.Concurrent;
using Reach.Relay;

namespace Reach.Module.Toolkit.ResultStore.Memory;

public sealed class MemoryResultStore<TResult> : IResultStore<TResult>
    where TResult : RelayCommandResult
{
    private readonly ConcurrentDictionary<string, ResultStoreEntry<TResult>> _store = [];

    public TimeSpan StalenessThreshold { get; }

    public MemoryResultStore(TimeSpan stalenessThreshold)
    {
        StalenessThreshold = stalenessThreshold;
    }

    public Task Insert(string id, CancellationToken cancellationToken)
    {
        var added = _store.TryAdd(
            id,
            new ResultStoreEntry<TResult>
            {
                Status = ResultStoreStatus.Ongoing,
                CreationTime = DateTimeOffset.Now,
            }
        );

        if (!added)
        {
            throw new ResultAlreadyExistsException(id);
        }

        return Task.CompletedTask;
    }

    public Task<ResultStoreEntry<TResult>?> GetValue(
        string id,
        CancellationToken cancellationToken
    ) => Task.FromResult(_store.GetValueOrDefault(id));

    public Task<bool> TrySetAsComplete(TResult result, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);

        return Task.FromResult(
            TryUpdate(
                result.CommandId,
                entry =>
                    entry with
                    {
                        Status = ResultStoreStatus.Complete,
                        Result = result,
                        ErrorMessage = null,
                    }
            )
        );
    }

    public Task<bool> TrySetAsFailed(
        string id,
        string error,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(
            TryUpdate(
                id,
                entry => entry with { Status = ResultStoreStatus.Failed, ErrorMessage = error }
            )
        );

    private bool TryUpdate(
        string key,
        Func<ResultStoreEntry<TResult>, ResultStoreEntry<TResult>> update
    )
    {
        while (true)
        {
            var existing = _store.GetValueOrDefault(key);

            if (existing is null)
            {
                return false;
            }

            if (_store.TryUpdate(key, update(existing), existing))
            {
                return true;
            }
        }
    }
}
