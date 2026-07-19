namespace Reach.Relay.Abstractions;

/// <summary>
/// A record that crosses a process boundary. <see cref="Kind" /> is the record's
/// stable, explicit wire name and is used as the type tag during (de)serialization.
/// </summary>
public interface IWireRecord
{
    static abstract string Kind { get; }
}

public abstract record RelayCommand(string Id)
{
    public string Version { get; } = "1";
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.Now;
}

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
