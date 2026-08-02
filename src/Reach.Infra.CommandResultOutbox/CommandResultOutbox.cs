using Reach.Infra.Persistence;
using Reach.Relay;

namespace Reach.Infra.CommandResultOutbox;

public sealed class CommandResultOutbox : IDbInitializable
{
    private readonly ICommandResultOutboxStore _store;

    public CommandResultOutbox(ICommandResultOutboxStore store)
    {
        _store = store;
    }

    public void EnsureCreated()
    {
        _store.EnsureCreated();
    }

    public Task CompleteCommandWithResult(
        RelayCommand command,
        RelayCommandResult result,
        CancellationToken cancellationToken
    )
    {
        if (command.Id != result.CommandId)
        {
            throw new InvalidOperationException(
                $"Command result {nameof(result.CommandId)} does not match command id"
            );
        }

        return _store.CompleteCommandWithResult(command, result, cancellationToken);
    }

    public Task<CommandResultOutboxItem?> PickPending(CancellationToken cancellationToken)
    {
        return _store.PickPending(cancellationToken);
    }

    public Task MarkPosted(string commandId, CancellationToken cancellationToken)
    {
        return _store.MarkPosted(commandId, cancellationToken);
    }

    public Task MarkPendingForRetry(string commandId, CancellationToken cancellationToken)
    {
        return _store.MarkPendingForRetry(commandId, cancellationToken);
    }
}
