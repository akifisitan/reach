using System.Data.Common;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Reach.Shared;

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

    public void Initialize()
    {
        _logger.ZLogDebug($"Initializing {nameof(DbConnectionFactory)}");

        SQLitePCL.Batteries.Init();

        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);

        using var connection = new SqliteConnection($"Data Source={_dbPath}");

        connection.Open();

        connection.Execute(
            """
            PRAGMA journal_mode=WAL;
            """
        );
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
