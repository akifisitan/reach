namespace Reach.Relay;

public interface ILocalRelay
{
    Task<PostToRemoteResult> PostToRemote(RelayCommand value, CancellationToken cancellationToken);

    IAsyncEnumerable<RelayCommandResult> PollFromRemote(
        PollingOptions? options = null,
        CancellationToken cancellationToken = default
    );
}

public interface IRemoteRelay
{
    Task<PostToLocalResult> PostToLocal(
        RelayCommandResult value,
        CancellationToken cancellationToken
    );

    IAsyncEnumerable<RelayCommand> PollFromLocal(
        PollingOptions? options = null,
        CancellationToken cancellationToken = default
    );
}
