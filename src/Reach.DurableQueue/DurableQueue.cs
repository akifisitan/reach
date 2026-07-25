using Reach.Relay;

namespace Reach.DurableQueue;

internal sealed class DurableQueue<T> : IDurableQueue<T>
{
    private readonly IRelayPayloadSerializer _payloadSerializer;
    private readonly IDurableQueueRepository _repository;
    private readonly Func<T, string> _idSelector;

    public DurableQueue(
        IRelayPayloadSerializer payloadSerializer,
        IDurableQueueRepository repository,
        Func<T, string> idSelector
    )
    {
        _payloadSerializer = payloadSerializer;
        _repository = repository;
        _idSelector = idSelector;
    }

    public Task Enqueue(T value, CancellationToken cancellationToken)
    {
        return _repository.Enqueue(
            new(_idSelector(value), _payloadSerializer.Serialize(value)),
            cancellationToken
        );
    }

    public async Task<T?> Pick(CancellationToken cancellationToken)
    {
        var item = await _repository.Pick(cancellationToken);

        return item is null ? default : _payloadSerializer.Deserialize<T>(item.EventData);
    }

    public async Task<T?> PickForRetry(CancellationToken cancellationToken)
    {
        var item = await _repository.PickForRetry(cancellationToken);

        return item is null ? default : _payloadSerializer.Deserialize<T>(item.EventData);
    }

    public Task Complete(T value, CancellationToken cancellationToken)
    {
        return _repository.Complete(_idSelector(value), cancellationToken);
    }

    public Task Fail(T value, CancellationToken cancellationToken)
    {
        return _repository.Fail(_idSelector(value), cancellationToken);
    }
}
