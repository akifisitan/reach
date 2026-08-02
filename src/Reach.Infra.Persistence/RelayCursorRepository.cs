using Dapper;
using Reach.Relay;

namespace Reach.Infra.Persistence;

internal sealed class RelayCursorRepository : IRelayCursorStorage, IDbInitializable
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<RelayCursorRepository> _logger;
    private readonly string _tableName;

    private const string TableColumns = "name,current_index,page_offset,creation_time";

    public RelayCursorRepository(
        DbConnectionFactory dbConnectionFactory,
        ILogger<RelayCursorRepository> logger,
        IOptions<RelayCursorRepositoryOptions> options
    )
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
        _tableName = options.Value.TableName;
    }

    public void EnsureCreated()
    {
        _logger.ZLogDebug($"Ensuring {_tableName} schema is created");

        using var connection = _dbConnectionFactory.CreateConnection();

        connection.Execute(
            $"""
            CREATE TABLE IF NOT EXISTS {_tableName}(
                name TEXT PRIMARY KEY,
                current_index INTEGER NOT NULL,
                page_offset INTEGER NOT NULL,
                creation_time TEXT NOT NULL
            ) STRICT;
            """
        );
    }

    public async Task Store(string name, RelayCursor cursor, CancellationToken cancellationToken)
    {
        await using var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var rows = await connection.ExecuteAsync(
            $"""
            UPDATE {_tableName}
            SET creation_time = $CreationTime, current_index = $CurrentIndex, page_offset = $PageOffset
            WHERE name = $Name
            """,
            new
            {
                Name = name,
                CreationTime = cursor.CreationTime.ToIsoString(),
                CurrentIndex = cursor.CurrentIndex,
                PageOffset = cursor.PageOffset,
            },
            transaction: transaction
        );

        if (rows == 0)
        {
            await connection.ExecuteAsync(
                $"""
                INSERT INTO {_tableName}(name, creation_time, current_index, page_offset)
                VALUES ($Name, $CreationTime, $CurrentIndex, $PageOffset)
                """,
                new
                {
                    Name = name,
                    CreationTime = cursor.CreationTime.ToIsoString(),
                    CurrentIndex = cursor.CurrentIndex,
                    PageOffset = cursor.PageOffset,
                },
                transaction: transaction
            );
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<RelayCursor?> Retrieve(string name, CancellationToken cancellationToken)
    {
        await using var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

        var result = await connection.QuerySingleOrDefaultAsync<DbRelayCursor>(
            $"""
            SELECT {TableColumns}
            FROM {_tableName}
            WHERE name = $Name
            LIMIT 1
            """,
            new { Name = name }
        );

        return result?.ToRelayCursor();
    }
}

internal sealed record DbRelayCursor(long CurrentIndex, long PageOffset, string CreationTime)
{
    public RelayCursor ToRelayCursor() =>
        new(CurrentIndex, DateTimeOffset.FromIsoString(CreationTime), PageOffset);
}

internal sealed record RelayCursorRepositoryOptions
{
    public required string TableName { get; set; }
}
