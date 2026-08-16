using FluentValidation;

namespace Reach.Module.Toolkit.ResultStore.Sqlite;

public sealed record SqliteResultStoreOptions
{
    public required string TableName { get; set; }
    public required TimeSpan StalenessThreshold { get; set; }
}

public sealed class SqliteResultStoreOptionsValidator : AbstractValidator<SqliteResultStoreOptions>
{
    public SqliteResultStoreOptionsValidator()
    {
        RuleFor(x => x.TableName).NotEmpty();
        RuleFor(x => x.StalenessThreshold).GreaterThan(TimeSpan.Zero);
    }
}
