using Reach.Relay;

namespace Reach.Module.Toolkit.ResultStore;

public interface IResultStore<TResult>
    where TResult : RelayCommandResult
{
    TimeSpan StalenessThreshold { get; }

    Task Insert(string id, CancellationToken cancellationToken);
    Task<ResultStoreEntry<TResult>?> GetValue(string id, CancellationToken cancellationToken);
    Task<bool> TrySetAsComplete(TResult result, CancellationToken cancellationToken);
    Task<bool> TrySetAsFailed(string id, string error, CancellationToken cancellationToken);
}

public sealed record ResultStoreEntry<TResult>
{
    public required ResultStoreStatus Status { get; init; }
    public TResult? Result { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTimeOffset CreationTime { get; init; }
}

public enum ResultStoreStatus
{
    New = 0,
    Ongoing = 1,
    Complete = 2,
    Failed = 3,
}

public sealed class ResultAlreadyExistsException(string Id)
    : Exception($"A result already exists for id '{Id}'");
