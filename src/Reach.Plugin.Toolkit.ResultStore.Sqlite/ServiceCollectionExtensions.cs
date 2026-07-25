using Reach.Persistence;
using Reach.Relay;

namespace Reach.Plugin.Toolkit.ResultStore.Sqlite;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqliteResultStore<TResult>(
        this IServiceCollection services,
        string tableName,
        TimeSpan stalenessThreshold
    )
        where TResult : RelayCommandResult
    {
        services.AddSingleton(sp => new SqliteResultStore<TResult>(
            sp.GetRequiredService<DbConnectionFactory>(),
            sp.GetRequiredService<ILogger<SqliteResultStore<TResult>>>(),
            new SqliteResultStoreOptions
            {
                TableName = tableName,
                StalenessThreshold = stalenessThreshold,
            },
            sp.GetRequiredService<IRelayPayloadSerializer>()
        ));
        services.AddSingleton<IResultStore<TResult>>(sp =>
            sp.GetRequiredService<SqliteResultStore<TResult>>()
        );
        services.AddSingleton<IDbInitializable>(sp =>
            sp.GetRequiredService<SqliteResultStore<TResult>>()
        );

        return services;
    }
}
