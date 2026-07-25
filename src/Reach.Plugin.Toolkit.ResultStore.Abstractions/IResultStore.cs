using Reach.Relay;

namespace Reach.Plugin.Toolkit.ResultStore;

public interface IResultStore<TResult>
    where TResult : RelayCommandResult
{
    TimeSpan StalenessThreshold { get; }

    Task Insert(string correlationId, CancellationToken cancellationToken);
    Task<ResultStoreEntry<TResult>?> GetValue(
        string correlationId,
        CancellationToken cancellationToken
    );
    Task<bool> TrySetAsComplete(TResult result, CancellationToken cancellationToken);
    Task<bool> TrySetAsFailed(
        string correlationId,
        string error,
        CancellationToken cancellationToken
    );
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
    NotFound = 0,
    Ongoing = 1,
    Complete = 2,
    Failed = 3,
}

public sealed class ResultAlreadyExistsException(string correlationId)
    : Exception($"A result already exists for correlation id '{correlationId}'");
