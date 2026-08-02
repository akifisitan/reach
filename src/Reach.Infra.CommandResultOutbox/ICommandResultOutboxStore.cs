using Reach.Infra.Persistence;
using Reach.Relay;

namespace Reach.Infra.CommandResultOutbox;

public interface ICommandResultOutboxStore : IDbInitializable
{
    Task CompleteCommandWithResult(
        RelayCommand command,
        RelayCommandResult result,
        CancellationToken cancellationToken
    );

    Task<CommandResultOutboxItem?> PickPending(CancellationToken cancellationToken);

    Task MarkPosted(string commandId, CancellationToken cancellationToken);

    Task MarkPendingForRetry(string commandId, CancellationToken cancellationToken);
}
