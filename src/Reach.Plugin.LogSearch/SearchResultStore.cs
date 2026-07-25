using System.Text.Json;
using Dapper;
using Microsoft.Data.Sqlite;
using Reach.Persistence;
using Reach.Plugin.LogSearch.Features.LogSearchStart;

namespace Reach.Plugin.LogSearch;

internal sealed class SearchResultStore : IDbInitializable
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<SearchResultStore> _logger;
    private readonly string _tableName;

    private const string TableColumns =
        "correlation_id,status,result_json,error_message,creation_time,completion_time";

    public TimeSpan StalenessThreshold { get; }

    public SearchResultStore(
        DbConnectionFactory dbConnectionFactory,
        ILogger<SearchResultStore> logger,
        IOptions<SearchResultStoreOptions> options
    )
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
        _tableName = options.Value.TableName;
        StalenessThreshold = options.Value.StalenessThreshold;
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
                result_json TEXT,
                error_message TEXT,
                creation_time TEXT NOT NULL,
                completion_time TEXT
            ) STRICT;
            """
        );
    }

    public async Task Insert(string key, CancellationToken cancellationToken)
    {
        await using var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

        try
        {
            await connection.ExecuteAsync(
                $"""
                INSERT INTO {_tableName}(correlation_id, status, result_json, error_message, creation_time, completion_time)
                VALUES ($CorrelationId, $Status, NULL, NULL, $CreationTime, NULL)
                """,
                new
                {
                    CorrelationId = key,
                    Status = SearchStatus.Ongoing,
                    CreationTime = DateTimeOffset.Now.ToIsoString(),
                }
            );
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new SearchResultAlreadyExistsException(key);
        }
    }

    public async Task<SearchResultStoreEntry?> GetValue(
        string key,
        CancellationToken cancellationToken
    )
    {
        await using var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

        var result = await connection.QuerySingleOrDefaultAsync<DbSearchResult>(
            $"""
            SELECT {TableColumns}
            FROM {_tableName}
            WHERE correlation_id = $CorrelationId
            LIMIT 1
            """,
            new { CorrelationId = key }
        );

        return result?.ToSearchResultStoreEntry();
    }

    public async Task<bool> TrySetAsComplete(
        SearchFileCommandResult result,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(result);

        string resultJson = JsonSerializer.Serialize(
            result,
            SearchResultStoreJsonSerializerContext.Default.SearchFileCommandResult
        );

        await using var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

        var rows = await connection.ExecuteAsync(
            $"""
            UPDATE {_tableName}
            SET status = $Status, result_json = $ResultJson, error_message = NULL, completion_time = $CompletionTime
            WHERE correlation_id = $CorrelationId
            """,
            new
            {
                CorrelationId = result.CommandId,
                Status = SearchStatus.Complete,
                ResultJson = resultJson,
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
                Status = SearchStatus.Failed,
                ErrorMessage = error,
                CompletionTime = DateTimeOffset.Now.ToIsoString(),
            }
        );

        return rows > 0;
    }
}

internal sealed record DbSearchResult(
    string CorrelationId,
    SearchStatus Status,
    string? ResultJson,
    string? ErrorMessage,
    string CreationTime,
    string? CompletionTime
)
{
    public SearchResultStoreEntry ToSearchResultStoreEntry() =>
        new()
        {
            Status = Status,
            Result = ResultJson is { } resultJson
                ? JsonSerializer.Deserialize(
                    resultJson,
                    SearchResultStoreJsonSerializerContext.Default.SearchFileCommandResult
                )
                : null,
            ErrorMessage = ErrorMessage,
            CreationTime = DateTimeOffset.FromIsoString(CreationTime),
        };
}

internal sealed record SearchResultStoreEntry
{
    public required SearchStatus Status { get; set; }
    public SearchFileCommandResult? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreationTime { get; set; }
}

internal enum SearchStatus
{
    NotFound = 0,
    Ongoing = 1,
    Complete = 2,
    Failed = 3,
}

internal sealed class SearchResultAlreadyExistsException(string correlationId)
    : Exception($"A search result already exists for correlation id '{correlationId}'");

internal sealed record SearchResultStoreOptions
{
    public required string TableName { get; set; }
    public required TimeSpan StalenessThreshold { get; set; }
}

[JsonSerializable(typeof(SearchFileCommandResult))]
internal sealed partial class SearchResultStoreJsonSerializerContext : JsonSerializerContext;
