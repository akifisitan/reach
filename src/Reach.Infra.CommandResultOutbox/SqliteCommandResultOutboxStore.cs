using Dapper;
using Microsoft.Data.Sqlite;
using Reach.Infra.DurableQueue;
using Reach.Infra.Persistence;
using Reach.Relay;

namespace Reach.Infra.CommandResultOutbox;

public sealed class SqliteCommandResultOutboxStore : ICommandResultOutboxStore
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly IDurableQueue<RelayCommand> _commandQueue;
    private readonly ILogger<SqliteCommandResultOutboxStore> _logger;
    private readonly IRelayPayloadSerializer _payloadSerializer;
    private readonly CommandResultOutboxOptions _options;
    private readonly SqliteCommandResultOutboxOptions _sqliteOptions;

    public SqliteCommandResultOutboxStore(
        DbConnectionFactory dbConnectionFactory,
        IDurableQueue<RelayCommand> commandQueue,
        ILogger<SqliteCommandResultOutboxStore> logger,
        IRelayPayloadSerializer payloadSerializer,
        IOptions<CommandResultOutboxOptions> options,
        IOptions<SqliteCommandResultOutboxOptions> sqliteOptions
    )
    {
        _dbConnectionFactory = dbConnectionFactory;
        _commandQueue = commandQueue;
        _logger = logger;
        _payloadSerializer = payloadSerializer;
        _options = options.Value;
        _sqliteOptions = sqliteOptions.Value;
    }

    public void EnsureCreated()
    {
        _logger.ZLogDebug($"Ensuring {_sqliteOptions.TableName} schema is created");

        using var connection = _dbConnectionFactory.CreateConnection();

        connection.Execute(
            $"""
            CREATE TABLE IF NOT EXISTS {_sqliteOptions.TableName}(
                command_id TEXT PRIMARY KEY,
                status INTEGER NOT NULL,
                result_data TEXT NOT NULL,
                creation_time TEXT NOT NULL,
                try_count INTEGER NOT NULL,
                last_try_time TEXT,
                next_try_time TEXT,
                posted_time TEXT,
                poison_time TEXT,
                poison_error TEXT
            ) STRICT;

            CREATE INDEX IF NOT EXISTS idx_{_sqliteOptions.TableName}_status_next_try_time ON {_sqliteOptions.TableName}(status, next_try_time);
            """
        );
    }

    public async Task CompleteCommandWithResult(
        RelayCommand command,
        RelayCommandResult result,
        CancellationToken cancellationToken
    )
    {
        var connection = await _dbConnectionFactory
            .CreateConnection(cancellationToken)
            .ConfigureAwait(false);
        await using var connectionScope = connection.ConfigureAwait(false);
        var transaction = await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var transactionScope = transaction.ConfigureAwait(false);

        try
        {
            await connection
                .ExecuteAsync(
                    $"""
                    INSERT INTO {_sqliteOptions.TableName}(command_id, status, result_data, creation_time, try_count, last_try_time, next_try_time, posted_time, poison_time, poison_error)
                    VALUES ($CommandId, $Status, $ResultData, $CreationTime, 0, NULL, NULL, NULL, NULL, NULL)
                    """,
                    new
                    {
                        CommandId = command.Id,
                        Status = CommandResultOutboxStatus.Pending,
                        ResultData = _payloadSerializer.Serialize(result),
                        CreationTime = DateTimeOffset.Now.ToIsoString(),
                    },
                    transaction: transaction
                )
                .ConfigureAwait(false);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            _logger.ZLogDebug($"Result outbox entry already exists for {command.Id}");
        }

        await _commandQueue
            .Complete(command, connection, transaction, cancellationToken)
            .ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<CommandResultOutboxItem?> PickPending(CancellationToken cancellationToken)
    {
        var connection = await _dbConnectionFactory
            .CreateConnection(cancellationToken)
            .ConfigureAwait(false);
        await using var connectionScope = connection.ConfigureAwait(false);

        var now = DateTimeOffset.Now;
        var record = await connection
            .QuerySingleOrDefaultAsync<DbCommandResultOutboxRecord>(
                $"""
                WITH next_record AS (
                    SELECT command_id
                    FROM {_sqliteOptions.TableName}
                    WHERE (
                        status = $PendingStatus
                        AND (next_try_time IS NULL OR next_try_time <= $Now)
                    )
                    OR (
                        status = $ProcessingStatus
                        AND last_try_time <= $ProcessingExpiredBefore
                    )
                    ORDER BY creation_time
                    LIMIT 1
                )
                UPDATE {_sqliteOptions.TableName}
                SET status = $NextStatus, try_count = try_count + 1, last_try_time = $Now, next_try_time = NULL
                WHERE command_id = (SELECT command_id FROM next_record)
                RETURNING command_id, result_data;
                """,
                new
                {
                    PendingStatus = CommandResultOutboxStatus.Pending,
                    ProcessingStatus = CommandResultOutboxStatus.Processing,
                    NextStatus = CommandResultOutboxStatus.Processing,
                    Now = now.ToIsoString(),
                    ProcessingExpiredBefore = now.Subtract(_options.ProcessingLease).ToIsoString(),
                }
            )
            .ConfigureAwait(false);

        if (record is null)
        {
            return null;
        }

        try
        {
            var result = _payloadSerializer.Deserialize<RelayCommandResult>(record.ResultData);

            if (record.CommandId != result.CommandId)
            {
                throw new InvalidOperationException(
                    $"Outbox command id {record.CommandId} does not match result command id {result.CommandId}"
                );
            }

            return new(record.CommandId, result);
        }
        catch (Exception ex)
        {
            await MarkPoisoned(record.CommandId, ex.Message, cancellationToken)
                .ConfigureAwait(false);

            throw;
        }
    }

    public async Task MarkPosted(string commandId, CancellationToken cancellationToken)
    {
        var connection = await _dbConnectionFactory
            .CreateConnection(cancellationToken)
            .ConfigureAwait(false);
        await using var connectionScope = connection.ConfigureAwait(false);

        await connection
            .ExecuteAsync(
                $"""
                UPDATE {_sqliteOptions.TableName}
                SET status = $Status, posted_time = $PostedTime, next_try_time = NULL
                WHERE command_id = $CommandId
                """,
                new
                {
                    CommandId = commandId,
                    Status = CommandResultOutboxStatus.Posted,
                    PostedTime = DateTimeOffset.Now.ToIsoString(),
                }
            )
            .ConfigureAwait(false);
    }

    public async Task MarkPendingForRetry(string commandId, CancellationToken cancellationToken)
    {
        var connection = await _dbConnectionFactory
            .CreateConnection(cancellationToken)
            .ConfigureAwait(false);
        await using var connectionScope = connection.ConfigureAwait(false);

        var record = await connection
            .QuerySingleAsync<DbOutboxRetryRecord>(
                $"""
                SELECT try_count
                FROM {_sqliteOptions.TableName}
                WHERE command_id = $CommandId
                """,
                new { CommandId = commandId }
            )
            .ConfigureAwait(false);

        var retryDelay = _options.RetryBaseDelay * Math.Pow(2, Math.Min(record.TryCount - 1, 10));

        if (retryDelay > _options.RetryMaxDelay)
        {
            retryDelay = _options.RetryMaxDelay;
        }

        await connection
            .ExecuteAsync(
                $"""
                UPDATE {_sqliteOptions.TableName}
                SET status = $Status, next_try_time = $NextTryTime
                WHERE command_id = $CommandId
                """,
                new
                {
                    CommandId = commandId,
                    Status = CommandResultOutboxStatus.Pending,
                    NextTryTime = DateTimeOffset.Now.Add(retryDelay).ToIsoString(),
                }
            )
            .ConfigureAwait(false);
    }

    private async Task MarkPoisoned(
        string commandId,
        string errorMessage,
        CancellationToken cancellationToken
    )
    {
        var connection = await _dbConnectionFactory
            .CreateConnection(cancellationToken)
            .ConfigureAwait(false);
        await using var connectionScope = connection.ConfigureAwait(false);

        await connection
            .ExecuteAsync(
                $"""
                UPDATE {_sqliteOptions.TableName}
                SET status = $Status, poison_time = $PoisonTime, poison_error = $PoisonError
                WHERE command_id = $CommandId
                """,
                new
                {
                    CommandId = commandId,
                    Status = CommandResultOutboxStatus.Poisoned,
                    PoisonTime = DateTimeOffset.Now.ToIsoString(),
                    PoisonError = errorMessage,
                }
            )
            .ConfigureAwait(false);
    }
}
