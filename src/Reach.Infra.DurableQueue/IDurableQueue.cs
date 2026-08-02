using System.Data.Common;

namespace Reach.Infra.DurableQueue;

public interface IDurableQueue<T>
{
    Task Enqueue(T value, CancellationToken cancellationToken);
    Task<T?> Pick(CancellationToken cancellationToken);
    Task<T?> PickForRetry(CancellationToken cancellationToken);
    Task Complete(T value, CancellationToken cancellationToken);
    Task Complete(
        T value,
        DbConnection connection,
        DbTransaction transaction,
        CancellationToken cancellationToken
    );
    Task Fail(T value, CancellationToken cancellationToken);
}
