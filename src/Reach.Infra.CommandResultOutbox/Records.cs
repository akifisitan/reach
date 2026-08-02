using Reach.Relay;

namespace Reach.Infra.CommandResultOutbox;

public sealed record CommandResultOutboxItem(string CommandId, RelayCommandResult Result);

public sealed record CommandResultOutboxOptions
{
    public TimeSpan ProcessingLease { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(2);
    public TimeSpan RetryMaxDelay { get; set; } = TimeSpan.FromMinutes(5);
}

public sealed record SqliteCommandResultOutboxOptions
{
    public required string TableName { get; set; }
}

internal sealed record DbCommandResultOutboxRecord(string CommandId, string ResultData);

internal sealed record DbOutboxRetryRecord(int TryCount);

internal enum CommandResultOutboxStatus
{
    Pending = 0,
    Processing = 1,
    Posted = 2,
    Poisoned = 3,
}
