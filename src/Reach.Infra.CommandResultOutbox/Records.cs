using FluentValidation;
using Reach.Relay;

namespace Reach.Infra.CommandResultOutbox;

public sealed record CommandResultOutboxItem(string CommandId, RelayCommandResult Result);

public sealed record CommandResultOutboxOptions
{
    public TimeSpan ProcessingLease { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(2);
    public TimeSpan RetryMaxDelay { get; set; } = TimeSpan.FromMinutes(5);
}

public sealed class CommandResultOutboxOptionsValidator
    : AbstractValidator<CommandResultOutboxOptions>
{
    public CommandResultOutboxOptionsValidator()
    {
        RuleFor(x => x.ProcessingLease).GreaterThan(TimeSpan.Zero);
        RuleFor(x => x.RetryBaseDelay).GreaterThan(TimeSpan.Zero);
        RuleFor(x => x.RetryMaxDelay)
            .GreaterThan(TimeSpan.Zero)
            .GreaterThanOrEqualTo(x => x.RetryBaseDelay);
    }
}

public sealed record SqliteCommandResultOutboxOptions
{
    public required string TableName { get; set; }
}

public sealed class SqliteCommandResultOutboxOptionsValidator
    : AbstractValidator<SqliteCommandResultOutboxOptions>
{
    public SqliteCommandResultOutboxOptionsValidator()
    {
        RuleFor(x => x.TableName).NotEmpty();
    }
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
