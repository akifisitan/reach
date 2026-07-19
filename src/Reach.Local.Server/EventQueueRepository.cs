using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Reach.Plugin.Abstractions;

namespace Reach.Local.Server;

internal interface IEventQueueRepository : IRepository
{
    Task QueueWork(QueueWorkItem item, CancellationToken cancellationToken);
    Task<WorkItem?> PickWork(CancellationToken cancellationToken);
    Task<WorkItem?> PickWorkForRetry(CancellationToken cancellationToken);
    Task UpdateWorkSuccess(string id, CancellationToken cancellationToken);
    Task UpdateWorkFailed(string id, CancellationToken cancellationToken);
}

internal sealed class EventQueueRepository : IEventQueueRepository
{
    private readonly string _tableName;
    private readonly int _maxTryCount;
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<EventQueueRepository> _logger;

    private const string TableColumns =
        "id,status,event_data,creation_time,try_count,last_try_time,next_try_time,completion_time";

    public EventQueueRepository(
        DbConnectionFactory dbConnectionFactory,
        ILogger<EventQueueRepository> logger,
        IOptions<EventQueueRepositoryOptions> options
    )
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
        _tableName = options.Value.TableName;
        _maxTryCount = options.Value.MaxTryCount;
    }

    public void Initialize()
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

    public async Task QueueWork(QueueWorkItem item, CancellationToken cancellationToken)
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
                    Status = WorkItemStatus.New,
                    EventData = item.Data,
                    CreationTime = DateTimeOffset.Now.ToIsoString(),
                    TryCount = 0,
                }
            );
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new AppErrors.AlreadyProcessed();
        }
    }

    public async Task<WorkItem?> PickWork(CancellationToken cancellationToken)
    {
        await using var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

        var result = await connection.QuerySingleOrDefaultAsync<DbWorkItem>(
            $"""
            WITH next_work AS (
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
            WHERE id = (SELECT id FROM next_work)
            RETURNING {TableColumns};
            """,
            new
            {
                Status = WorkItemStatus.New,
                CreationTime = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(1)).ToIsoString(),
                NextStatus = WorkItemStatus.Processing,
                NextLastTryTime = DateTimeOffset.Now.ToIsoString(),
            }
        );

        return result?.ToWorkItem();
    }

    public async Task<WorkItem?> PickWorkForRetry(CancellationToken cancellationToken)
    {
        await using var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

        var result = await connection.QuerySingleOrDefaultAsync<DbWorkItem>(
            $"""
            WITH next_work AS (
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
            WHERE id = (SELECT id FROM next_work)
            RETURNING {TableColumns};
            """,
            new
            {
                Status = WorkItemStatus.Error,
                NextTryTime = DateTimeOffset.Now.ToIsoString(), // how are we going to handle entries from before?
                UpdatedStatus = WorkItemStatus.Processing,
                UpdatedLastTryTime = DateTimeOffset.Now.ToIsoString(),
            }
        );

        return result?.ToWorkItem();
    }

    public async Task UpdateWorkSuccess(string id, CancellationToken cancellationToken)
    {
        await using var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

        await connection.ExecuteAsync(
            $"""
            UPDATE {_tableName}
            SET status = $Status, completion_time = $CompletionTime
            WHERE id = $Id
            """,
            new
            {
                Id = id,
                Status = WorkItemStatus.Success,
                CompletionTime = DateTimeOffset.Now.ToIsoString(),
            }
        );
    }

    public async Task UpdateWorkFailed(string id, CancellationToken cancellationToken)
    {
        await using var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var item = await connection.QuerySingleAsync<DbWorkItem>(
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
                new { Id = id, Status = WorkItemStatus.Expired },
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
                    Status = WorkItemStatus.Error,
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

internal sealed record DbWorkItem(
    string Id,
    WorkItemStatus Status,
    string EventData,
    int TryCount,
    string CreationTime,
    string? LastTryTime,
    string? NextTryTime,
    string? CompletionTime
)
{
    public WorkItem ToWorkItem() =>
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

internal sealed record WorkItem(
    string Id,
    WorkItemStatus Status,
    string EventData,
    int TryCount,
    DateTimeOffset CreationTime,
    DateTimeOffset? LastTryTime,
    DateTimeOffset? NextTryTime,
    DateTimeOffset? CompletionTime
);

internal enum WorkItemStatus
{
    New = 0,
    Processing = 1,
    Success = 2,
    Error = 3,
    Expired = 4,
}

internal sealed record QueueWorkItem(string Id, string Data);

internal sealed record EventQueueRepositoryOptions
{
    public required int MaxTryCount { get; set; } = 4;
    public required string TableName { get; set; }
}
