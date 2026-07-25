using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace Reach.Infra.Persistence;

public sealed class DbConnectionFactory
{
    private readonly string _dbPath;
    private readonly ILogger<DbConnectionFactory> _logger;

    public DbConnectionFactory(
        ILogger<DbConnectionFactory> logger,
        IOptions<DbConnectionFactoryOptions> options
    )
    {
        _logger = logger;
        _dbPath = options.Value.DbPath;
    }

    public DbConnection CreateConnection()
    {
        var connection = new SqliteConnection($"Data Source={_dbPath}");

        _logger.ZLogDebug($"Opening database connection");

        connection.Open();

        return connection;
    }

    public async Task<DbConnection> CreateConnection(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection($"Data Source={_dbPath}");

        _logger.ZLogDebug($"Opening database connection");

        await connection.OpenAsync(cancellationToken);

        return connection;
    }
}

public sealed class DbConnectionFactoryOptions
{
    public required string DbPath { get; set; }
}
