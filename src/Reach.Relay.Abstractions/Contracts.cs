namespace Reach.Relay.Abstractions;

public abstract record RelayCommand(string Kind, string Id)
{
    public string Version { get; } = "1";
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.Now;
}

public abstract record RelayCommand<TResult>(string Kind, string Id) : RelayCommand(Kind, Id)
    where TResult : RelayCommandResult;

public abstract record RelayCommandResult(string Id, RelayCommand Command)
{
    public string Version { get; } = "1";
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.Now;
}

public sealed record PostToRemoteResult(PollId PollId);

public sealed record PostToLocalResult();

public sealed record PollId(string Value);

public sealed record PollingOptions();

public interface IRelayPayloadSerializer
{
    string Serialize<T>(T value);
    T Deserialize<T>(string payload);
}

public record RelayCursor(int CurrentIndex, DateTimeOffset CreationTime);

public interface IRelayCursorStorage
{
    Task Store(RelayCursor cursor, CancellationToken cancellationToken);
    Task<RelayCursor?> Retrieve(CancellationToken cancellationToken);
}
