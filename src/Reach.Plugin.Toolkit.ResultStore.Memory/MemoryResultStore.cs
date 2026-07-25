using System.Collections.Concurrent;
using Reach.Plugin.Toolkit.ResultStore.Abstractions;
using Reach.Relay.Abstractions;

namespace Reach.Plugin.Toolkit.ResultStore.Memory;

public sealed class MemoryResultStore<TResult> : IResultStore<TResult>
    where TResult : RelayCommandResult
{
    private readonly ConcurrentDictionary<string, ResultStoreEntry<TResult>> _store = [];

    public TimeSpan StalenessThreshold { get; }

    public MemoryResultStore(TimeSpan stalenessThreshold)
    {
        StalenessThreshold = stalenessThreshold;
    }

    public Task Insert(string correlationId, CancellationToken cancellationToken)
    {
        var added = _store.TryAdd(
            correlationId,
            new ResultStoreEntry<TResult>
            {
                Status = ResultStoreStatus.Ongoing,
                CreationTime = DateTimeOffset.Now,
            }
        );

        if (!added)
        {
            throw new ResultAlreadyExistsException(correlationId);
        }

        return Task.CompletedTask;
    }

    public Task<ResultStoreEntry<TResult>?> GetValue(
        string correlationId,
        CancellationToken cancellationToken
    ) => Task.FromResult(_store.GetValueOrDefault(correlationId));

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
        string correlationId,
        string error,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(
            TryUpdate(
                correlationId,
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
