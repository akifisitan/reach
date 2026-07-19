using Reach.Relay.Abstractions;

namespace Reach.Local.Server;

internal interface IEventQueue
{
    Task QueueWork(RelayCommandResult value, CancellationToken cancellationToken);
    Task<RelayCommandResult?> Pick(CancellationToken cancellationToken);
    Task<RelayCommandResult?> PickForRetry(CancellationToken cancellationToken);
    Task SetAsSuccess(RelayCommandResult value, CancellationToken cancellationToken);
    Task SetAsFailed(RelayCommandResult value, CancellationToken cancellationToken);
}

internal sealed class EventQueue : IEventQueue
{
    private readonly IRelayPayloadSerializer _payloadSerializer;
    private readonly IEventQueueRepository _repository;

    public EventQueue(IRelayPayloadSerializer payloadSerializer, IEventQueueRepository repository)
    {
        _payloadSerializer = payloadSerializer;
        _repository = repository;
    }

    public async Task QueueWork(RelayCommandResult value, CancellationToken cancellationToken)
    {
        await _repository.QueueWork(
            new(value.Id, _payloadSerializer.Serialize(value)),
            cancellationToken
        );
    }

    public async Task<RelayCommandResult?> Pick(CancellationToken cancellationToken)
    {
        var item = await _repository.PickWork(cancellationToken);

        return item is null
            ? null
            : _payloadSerializer.Deserialize<RelayCommandResult>(item.EventData);
    }

    public async Task<RelayCommandResult?> PickForRetry(CancellationToken cancellationToken)
    {
        var item = await _repository.PickWorkForRetry(cancellationToken);

        return item is null
            ? null
            : _payloadSerializer.Deserialize<RelayCommandResult>(item.EventData);
    }

    public Task SetAsSuccess(RelayCommandResult value, CancellationToken cancellationToken)
    {
        return _repository.UpdateWorkSuccess(value.Id, cancellationToken);
    }

    public Task SetAsFailed(RelayCommandResult value, CancellationToken cancellationToken)
    {
        return _repository.UpdateWorkFailed(value.Id, cancellationToken);
    }
}
