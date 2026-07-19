using Dapper;
using Reach.Plugin.Abstractions;
using Reach.Relay.Abstractions;

namespace Reach.Local.Server;

internal sealed class RelayCursorRepository : IRelayCursorStorage, IRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<RelayCursorRepository> _logger;

    private const string TableName = "relay_cursor_settings";

    private const string TableColumns = "name,current_index,creation_time";

    public RelayCursorRepository(
        DbConnectionFactory dbConnectionFactory,
        ILogger<RelayCursorRepository> logger
    )
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    public void Initialize()
    {
        _logger.ZLogDebug($"Ensuring {TableName} schema is created");

        using var connection = _dbConnectionFactory.CreateConnection();

        connection.Execute(
            $"""
            CREATE TABLE IF NOT EXISTS {TableName}(
                name TEXT PRIMARY KEY,
                current_index INTEGER NOT NULL,
                creation_time TEXT NOT NULL
            ) STRICT;
            """
        );
    }

    public async Task Store(RelayCursor cursor, CancellationToken cancellationToken)
    {
        await using var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var rows = await connection.ExecuteAsync(
            $"""
            UPDATE {TableName}
            SET creation_time = $CreationTime, current_index = $CurrentIndex
            WHERE name = $Name
            """,
            new
            {
                Name = "cursor",
                CreationTime = cursor.CreationTime,
                CurrentIndex = cursor.CurrentIndex,
            },
            transaction: transaction
        );

        if (rows == 0)
        {
            await connection.ExecuteAsync(
                $"""
                INSERT INTO {TableName}(name, creation_time, current_index)
                VALUES ($Name, $CreationTime, $CurrentIndex)
                """,
                new
                {
                    Name = "cursor",
                    CreationTime = cursor.CreationTime.ToIsoString(),
                    CurrentIndex = cursor.CurrentIndex,
                },
                transaction: transaction
            );
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<RelayCursor?> Retrieve(CancellationToken cancellationToken)
    {
        await using var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

        var result = await connection.QuerySingleOrDefaultAsync<DbRelayCursor>(
            $"""
            SELECT {TableColumns}
            FROM {TableName}
            WHERE name = $Name
            LIMIT 1
            """,
            new { Name = "cursor" }
        );

        return result is null
            ? null
            : new RelayCursor(
                result.CurrentIndex,
                DateTimeOffset.FromIsoString(result.CreationTime)
            );
    }
}

internal sealed record DbRelayCursor(int CurrentIndex, string CreationTime);
