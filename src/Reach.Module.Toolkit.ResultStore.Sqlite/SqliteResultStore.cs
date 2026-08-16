using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Reach.Infra.Persistence;
using Reach.Relay;

namespace Reach.Module.Toolkit.ResultStore.Sqlite;

public sealed class SqliteResultStore<TResult> : IResultStore<TResult>, IDbInitializable
    where TResult : RelayCommandResult
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<SqliteResultStore<TResult>> _logger;
    private readonly IRelayPayloadSerializer _payloadSerializer;
    private readonly string _tableName;

    private const string TableColumns =
        "id,status,result_data,error_message,creation_time,completion_time";

    public TimeSpan StalenessThreshold { get; }

    public SqliteResultStore(
        DbConnectionFactory dbConnectionFactory,
        ILogger<SqliteResultStore<TResult>> logger,
        IOptions<SqliteResultStoreOptions> options,
        IRelayPayloadSerializer payloadSerializer
    )
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
        _tableName = options.Value.TableName;
        StalenessThreshold = options.Value.StalenessThreshold;
        _payloadSerializer = payloadSerializer;
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
                result_data TEXT,
                error_message TEXT,
                creation_time TEXT NOT NULL,
                completion_time TEXT
            ) STRICT;
            """
        );
    }

    public async Task Insert(string id, CancellationToken cancellationToken)
    {
        await using var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

        try
        {
            await connection.ExecuteAsync(
                $"""
                INSERT INTO {_tableName}(id, status, result_data, error_message, creation_time, completion_time)
                VALUES ($Id, $Status, NULL, NULL, $CreationTime, NULL)
                """,
                new
                {
                    Id = id,
                    Status = ResultStoreStatus.Ongoing,
                    CreationTime = DateTimeOffset.Now.ToIsoString(),
                }
            );
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new ResultAlreadyExistsException(id);
        }
    }

    public async Task<ResultStoreEntry<TResult>?> GetValue(
        string id,
        CancellationToken cancellationToken
    )
    {
        await using var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

        var result = await connection.QuerySingleOrDefaultAsync<DbResultStoreEntry>(
            $"""
            SELECT {TableColumns}
            FROM {_tableName}
            WHERE id = $Id
            LIMIT 1
            """,
            new { Id = id }
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
            SET status = $Status, result_data = $ResultData, error_message = NULL, completion_time = $CompletionTime
            WHERE id = $Id
            """,
            new
            {
                Id = result.CommandId,
                Status = ResultStoreStatus.Complete,
                ResultData = data,
                CompletionTime = DateTimeOffset.Now.ToIsoString(),
            }
        );

        return rows > 0;
    }

    public async Task<bool> TrySetAsFailed(
        string id,
        string error,
        CancellationToken cancellationToken
    )
    {
        await using var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

        var rows = await connection.ExecuteAsync(
            $"""
            UPDATE {_tableName}
            SET status = $Status, error_message = $ErrorMessage, completion_time = $CompletionTime
            WHERE id = $Id
            """,
            new
            {
                Id = id,
                Status = ResultStoreStatus.Failed,
                ErrorMessage = error,
                CompletionTime = DateTimeOffset.Now.ToIsoString(),
            }
        );

        return rows > 0;
    }
}

internal sealed record DbResultStoreEntry(
    string Id,
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
