namespace Reach.DurableQueue;

public interface IDurableQueue<T>
{
    Task Enqueue(T value, CancellationToken cancellationToken);
    Task<T?> Pick(CancellationToken cancellationToken);
    Task<T?> PickForRetry(CancellationToken cancellationToken);
    Task Complete(T value, CancellationToken cancellationToken);
    Task Fail(T value, CancellationToken cancellationToken);
}
