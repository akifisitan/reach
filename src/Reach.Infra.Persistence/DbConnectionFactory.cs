using System.Data.Common;
using FluentValidation;
using Microsoft.Data.Sqlite;

namespace Reach.Infra.Persistence;

public sealed class DbConnectionFactory
{
    private readonly string _connectionString;
    private readonly ILogger<DbConnectionFactory> _logger;

    public DbConnectionFactory(
        ILogger<DbConnectionFactory> logger,
        IOptions<DbConnectionFactoryOptions> options
    )
    {
        _logger = logger;
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = options.Value.DbPath,
        }.ConnectionString;
    }

    public DbConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);

        _logger.ZLogTrace($"Opening database connection");

        connection.Open();

        return connection;
    }

    public async Task<DbConnection> CreateConnection(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);

        _logger.ZLogTrace($"Opening database connection");

        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return connection;
    }
}

public sealed class DbConnectionFactoryOptions
{
    public required string DbPath { get; set; }
}

public sealed class DbConnectionFactoryOptionsValidator
    : AbstractValidator<DbConnectionFactoryOptions>
{
    public DbConnectionFactoryOptionsValidator()
    {
        RuleFor(x => x.DbPath).NotEmpty().Must(Path.IsPathRooted);
    }
}
