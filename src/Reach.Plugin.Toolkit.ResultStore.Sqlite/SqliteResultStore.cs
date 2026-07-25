using Dapper;
using Microsoft.Data.Sqlite;
using Reach.Infra.Persistence;
using Reach.Relay;

namespace Reach.Plugin.Toolkit.ResultStore.Sqlite;

public sealed class SqliteResultStore<TResult> : IResultStore<TResult>, IDbInitializable
    where TResult : RelayCommandResult
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<SqliteResultStore<TResult>> _logger;
    private readonly IRelayPayloadSerializer _payloadSerializer;
    private readonly string _tableName;

    private const string TableColumns =
        "correlation_id,status,result_data,error_message,creation_time,completion_time";

    public TimeSpan StalenessThreshold { get; }

    public SqliteResultStore(
        DbConnectionFactory dbConnectionFactory,
        ILogger<SqliteResultStore<TResult>> logger,
        SqliteResultStoreOptions options,
        IRelayPayloadSerializer payloadSerializer
    )
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
        _tableName = options.TableName;
        StalenessThreshold = options.StalenessThreshold;
        _payloadSerializer = payloadSerializer;
    }

    public void EnsureCreated()
    {
        _logger.ZLogDebug($"Ensuring {_tableName} schema is created");

        using var connection = _dbConnectionFactory.CreateConnection();

        connection.Execute(
            $"""
            CREATE TABLE IF NOT EXISTS {_tableName}(
                correlation_id TEXT PRIMARY KEY,
                status INTEGER NOT NULL,
                result_data TEXT,
                error_message TEXT,
                creation_time TEXT NOT NULL,
                completion_time TEXT
            ) STRICT;
            """
        );
    }

    public async Task Insert(string correlationId, CancellationToken cancellationToken)
    {
        await using var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

        try
        {
            await connection.ExecuteAsync(
                $"""
                INSERT INTO {_tableName}(correlation_id, status, result_data, error_message, creation_time, completion_time)
                VALUES ($CorrelationId, $Status, NULL, NULL, $CreationTime, NULL)
                """,
                new
                {
                    CorrelationId = correlationId,
                    Status = ResultStoreStatus.Ongoing,
                    CreationTime = DateTimeOffset.Now.ToIsoString(),
                }
            );
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new ResultAlreadyExistsException(correlationId);
        }
    }

    public async Task<ResultStoreEntry<TResult>?> GetValue(
        string correlationId,
        CancellationToken cancellationToken
    )
    {
        await using var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

        var result = await connection.QuerySingleOrDefaultAsync<DbResultStoreEntry>(
            $"""
            SELECT {TableColumns}
            FROM {_tableName}
            WHERE correlation_id = $CorrelationId
            LIMIT 1
            """,
            new { CorrelationId = correlationId }
        );

        return result?.ToEntry(_payloadSerializer.Deserialize<TResult>);
    }

    public async Task<bool> TrySetAsComplete(TResult result, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);

        var data = _payloadSerializer.Serialize(result);

        await using var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

        var rows = await connection.ExecuteAsync(
            $"""
            UPDATE {_tableName}
            SET status = $Status, result_data = $ResultJson, error_message = NULL, completion_time = $CompletionTime
            WHERE correlation_id = $CorrelationId
            """,
            new
            {
                CorrelationId = result.CommandId,
                Status = ResultStoreStatus.Complete,
                ResultJson = data,
                CompletionTime = DateTimeOffset.Now.ToIsoString(),
            }
        );

        return rows > 0;
    }

    public async Task<bool> TrySetAsFailed(
        string correlationId,
        string error,
        CancellationToken cancellationToken
    )
    {
        await using var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

        var rows = await connection.ExecuteAsync(
            $"""
            UPDATE {_tableName}
            SET status = $Status, error_message = $ErrorMessage, completion_time = $CompletionTime
            WHERE correlation_id = $CorrelationId
            """,
            new
            {
                CorrelationId = correlationId,
                Status = ResultStoreStatus.Failed,
                ErrorMessage = error,
                CompletionTime = DateTimeOffset.Now.ToIsoString(),
            }
        );

        return rows > 0;
    }
}

internal sealed record DbResultStoreEntry(
    string CorrelationId,
    ResultStoreStatus Status,
    string? ResultData,
    string? ErrorMessage,
    string CreationTime,
    string? CompletionTime
)
{
    public ResultStoreEntry<TResult> ToEntry<TResult>(Func<string, TResult> deserializer)
        where TResult : RelayCommandResult =>
        new()
        {
            Status = Status,
            Result = ResultData is { } resultData ? deserializer(resultData) : null,
            ErrorMessage = ErrorMessage,
            CreationTime = DateTimeOffset.FromIsoString(CreationTime),
        };
}

public sealed record SqliteResultStoreOptions
{
    public required string TableName { get; set; }
    public required TimeSpan StalenessThreshold { get; set; }
}
