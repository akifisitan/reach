namespace Reach.Relay;

public sealed class RelayDelivery<T>
{
    private readonly Func<CancellationToken, Task> _commit;

    public RelayDelivery(T value, Func<CancellationToken, Task> commit)
    {
        Value = value;
        _commit = commit;
    }

    public T Value { get; }

    public Task Commit(CancellationToken cancellationToken)
    {
        return _commit(cancellationToken);
    }
}
