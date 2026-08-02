using System.Data.Common;
using Dapper;
using Microsoft.Data.Sqlite;
using Reach.Infra.Persistence;

namespace Reach.Infra.DurableQueue;

internal interface IDurableQueueRepository : IDbInitializable
{
    Task Enqueue(EnqueueRecord item, CancellationToken cancellationToken);
    Task<QueueRecord?> Pick(CancellationToken cancellationToken);
    Task<QueueRecord?> PickForRetry(CancellationToken cancellationToken);
    Task Complete(string id, CancellationToken cancellationToken);
    Task Complete(
        string id,
        DbConnection connection,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    );
    Task Fail(string id, CancellationToken cancellationToken);
}

internal sealed class DurableQueueRepository : IDurableQueueRepository
{
    private readonly string _tableName;
    private readonly int _maxTryCount;
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<DurableQueueRepository> _logger;

    private const string TableColumns =
        "id,status,event_data,creation_time,try_count,last_try_time,next_try_time,completion_time";

    public DurableQueueRepository(
        DbConnectionFactory dbConnectionFactory,
        ILogger<DurableQueueRepository> logger,
        IOptions<DurableQueueOptions> options
    )
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
        _tableName = options.Value.TableName;
        _maxTryCount = options.Value.MaxTryCount;
    }

    public void EnsureCreated()
    {
        _logger.ZLogDebug($"Ensuring {_tableName} schema is created");

        using var connection = _dbConnectionFactory.CreateConnection();

        connection.Execute(
            $"""
            CREATE TABLE IF NOT EXISTS {_tableName}(
                id TEXT PRIMARY KEY,
                status INTEGER NOT NULL,
                event_data TEXT NOT NULL,
                creation_time TEXT NOT NULL,
                try_count INTEGER NOT NULL,
                last_try_time TEXT,
                next_try_time TEXT,
                completion_time TEXT
            ) STRICT;

            CREATE INDEX IF NOT EXISTS idx_{_tableName}_creation_time_status ON {_tableName}(creation_time, status);
            CREATE INDEX IF NOT EXISTS idx_{_tableName}_next_try_time_status ON {_tableName}(next_try_time, status);
            """
        );
    }

    public async Task Enqueue(EnqueueRecord item, CancellationToken cancellationToken)
    {
        await using var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

        try
        {
            await connection.ExecuteAsync(
                $"""
                INSERT INTO {_tableName}(id, status, event_data, creation_time, try_count, last_try_time, next_try_time, completion_time)
                VALUES ($Id, $Status, $EventData, $CreationTime, $TryCount, NULL, NULL, NULL)
                """,
                new
                {
                    Id = item.Id,
                    Status = QueueRecordStatus.New,
                    EventData = item.Data,
                    CreationTime = DateTimeOffset.Now.ToIsoString(),
                    TryCount = 0,
                }
            );
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new AlreadyQueuedException();
        }
    }

    public async Task<QueueRecord?> Pick(CancellationToken cancellationToken)
    {
        await using var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

        var result = await connection.QuerySingleOrDefaultAsync<DbQueueRecord>(
            $"""
            WITH next_record AS (
                SELECT {TableColumns}
                FROM {_tableName}
                WHERE 1=1
                AND status = $Status
                AND creation_time >= $CreationTime
                AND try_count = 0
                ORDER BY id
                LIMIT 1
            )
            UPDATE {_tableName}
            SET status = $NextStatus, try_count = 1, last_try_time = $NextLastTryTime, next_try_time = NULL
            WHERE id = (SELECT id FROM next_record)
            RETURNING {TableColumns};
            """,
            new
            {
                Status = QueueRecordStatus.New,
                CreationTime = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(1)).ToIsoString(),
                NextStatus = QueueRecordStatus.Processing,
                NextLastTryTime = DateTimeOffset.Now.ToIsoString(),
            }
        );

        return result?.ToQueueRecord();
    }

    public async Task<QueueRecord?> PickForRetry(CancellationToken cancellationToken)
    {
        await using var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

        var result = await connection.QuerySingleOrDefaultAsync<DbQueueRecord>(
            $"""
            WITH next_record AS (
                SELECT id
                FROM {_tableName}
                WHERE 1=1
                AND status = $Status
                AND next_try_time <= $NextTryTime
                AND try_count < {_maxTryCount}
                ORDER BY creation_time
                LIMIT 1
            )
            UPDATE {_tableName}
            SET status = $UpdatedStatus, last_try_time = $UpdatedLastTryTime, try_count = try_count + 1
            WHERE id = (SELECT id FROM next_record)
            RETURNING {TableColumns};
            """,
            new
            {
                Status = QueueRecordStatus.Error,
                NextTryTime = DateTimeOffset.Now.ToIsoString(),
                UpdatedStatus = QueueRecordStatus.Processing,
                UpdatedLastTryTime = DateTimeOffset.Now.ToIsoString(),
            }
        );

        return result?.ToQueueRecord();
    }

    public async Task Complete(string id, CancellationToken cancellationToken)
    {
        await using var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

        await Complete(id, connection, transaction: null, cancellationToken);
    }

    public async Task Complete(
        string id,
        DbConnection connection,
        DbTransaction? transaction,
        CancellationToken cancellationToken
    )
    {
        await connection.ExecuteAsync(
            $"""
            UPDATE {_tableName}
            SET status = $Status, completion_time = $CompletionTime
            WHERE id = $Id
            """,
            new
            {
                Id = id,
                Status = QueueRecordStatus.Success,
                CompletionTime = DateTimeOffset.Now.ToIsoString(),
            },
            transaction: transaction
        );
    }

    public async Task Fail(string id, CancellationToken cancellationToken)
    {
        await using var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var item = await connection.QuerySingleAsync<DbQueueRecord>(
            $"""
            SELECT {TableColumns}
            FROM {_tableName}
            WHERE id = $Id
            """,
            new { Id = id },
            transaction: transaction
        );

        if (item.TryCount >= _maxTryCount)
        {
            await connection.ExecuteAsync(
                $"""
                UPDATE {_tableName}
                SET status = $Status, next_try_time = NULL
                WHERE id = $Id
                """,
                new { Id = id, Status = QueueRecordStatus.Expired },
                transaction: transaction
            );
        }
        else
        {
            await connection.ExecuteAsync(
                $"""
                UPDATE {_tableName}
                SET status = $Status, next_try_time = $NextTryTime
                WHERE id = $Id
                """,
                new
                {
                    Id = id,
                    Status = QueueRecordStatus.Error,
                    NextTryTime = DateTimeOffset
                        .Now.Add(TimeSpan.FromSeconds(10 * item.TryCount))
                        .ToIsoString(),
                },
                transaction: transaction
            );
        }

        await transaction.CommitAsync(cancellationToken);
    }
}

internal sealed record DbQueueRecord(
    string Id,
    QueueRecordStatus Status,
    string EventData,
    int TryCount,
    string CreationTime,
    string? LastTryTime,
    string? NextTryTime,
    string? CompletionTime
)
{
    public QueueRecord ToQueueRecord() =>
        new(
            Id: Id,
            TryCount: TryCount,
            Status: Status,
            EventData: EventData,
            CreationTime: DateTimeOffset.FromIsoString(CreationTime),
            LastTryTime: LastTryTime is { } lastTryTime
                ? DateTimeOffset.FromIsoString(lastTryTime)
                : null,
            NextTryTime: NextTryTime is { } nextTryTime
                ? DateTimeOffset.FromIsoString(nextTryTime)
                : null,
            CompletionTime: CompletionTime is { } completionTime
                ? DateTimeOffset.FromIsoString(completionTime)
                : null
        );
}

internal sealed record QueueRecord(
    string Id,
    QueueRecordStatus Status,
    string EventData,
    int TryCount,
    DateTimeOffset CreationTime,
    DateTimeOffset? LastTryTime,
    DateTimeOffset? NextTryTime,
    DateTimeOffset? CompletionTime
);

internal enum QueueRecordStatus
{
    New = 0,
    Processing = 1,
    Success = 2,
    Error = 3,
    Expired = 4,
}

internal sealed record EnqueueRecord(string Id, string Data);
