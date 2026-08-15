namespace Reach.Relay;

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

public abstract record RelayCommandResult(string CommandId)
{
    public string Version { get; } = "1";
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.Now;
}

public interface IRelayPayloadSerializer
{
    string Serialize<T>(T value);
    T Deserialize<T>(string payload);
}

public interface IRelayCursorStorage
{
    Task Store(string name, RelayCursor cursor, CancellationToken cancellationToken);
    Task<RelayCursor?> Retrieve(string name, CancellationToken cancellationToken);
}
