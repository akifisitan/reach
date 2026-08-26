using System.Data.Common;

namespace Reach.Infra.DurableQueue;

internal sealed class DurableQueue<T> : IDurableQueue<T>
{
    private readonly IDurableQueueDataSerializer _serializer;
    private readonly IDurableQueueRepository _repository;
    private readonly Func<T, string> _idSelector;

    public DurableQueue(
        IDurableQueueDataSerializer serializer,
        IDurableQueueRepository repository,
        Func<T, string> idSelector
    )
    {
        _serializer = serializer;
        _repository = repository;
        _idSelector = idSelector;
    }

    public Task Enqueue(T value, CancellationToken cancellationToken)
    {
        return _repository.Enqueue(
            new(_idSelector(value), _serializer.Serialize(value)),
            cancellationToken
        );
    }

    public async Task<T?> Pick(CancellationToken cancellationToken)
    {
        var item = await _repository.Pick(cancellationToken).ConfigureAwait(false);

        return item is null ? default : _serializer.Deserialize<T>(item.EventData);
    }

    public async Task<T?> PickForRetry(CancellationToken cancellationToken)
    {
        var item = await _repository.PickForRetry(cancellationToken).ConfigureAwait(false);

        return item is null ? default : _serializer.Deserialize<T>(item.EventData);
    }

    public Task Complete(T value, CancellationToken cancellationToken)
    {
        return _repository.Complete(_idSelector(value), cancellationToken);
    }

    public Task Complete(
        T value,
        DbConnection connection,
        DbTransaction transaction,
        CancellationToken cancellationToken
    )
    {
        return _repository.Complete(_idSelector(value), connection, transaction, cancellationToken);
    }

    public Task Fail(T value, CancellationToken cancellationToken)
    {
        return _repository.Fail(_idSelector(value), cancellationToken);
    }
}
