namespace Reach.Relay;

public interface ILocalRelay
{
    Task<PostToRemoteResult> PostToRemote(RelayCommand value, CancellationToken cancellationToken);

    IAsyncEnumerable<RelayDelivery<RelayCommandResult>> PollFromRemote(
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

    IAsyncEnumerable<RelayDelivery<RelayCommand>> PollFromLocal(
        PollingOptions? options = null,
        CancellationToken cancellationToken = default
    );
}
