using Reach.Infra.Persistence;
using Reach.Options;
using Reach.Relay;

namespace Reach.Module.Toolkit.ResultStore.Sqlite;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqliteResultStore<TResult>(
        this IServiceCollection services,
        string tableName,
        TimeSpan stalenessThreshold
    )
        where TResult : RelayCommandResult
    {
        services
            .AddOptions<SqliteResultStoreOptions>()
            .Configure(options =>
            {
                options.TableName = tableName;
                options.StalenessThreshold = stalenessThreshold;
            })
            .WithValidator<SqliteResultStoreOptions, SqliteResultStoreOptionsValidator>();
        services.AddSingleton<SqliteResultStore<TResult>>();
        services.AddSingleton<IResultStore<TResult>>(sp =>
            sp.GetRequiredService<SqliteResultStore<TResult>>()
        );
        services.AddSingleton<IDbInitializable>(sp =>
            sp.GetRequiredService<SqliteResultStore<TResult>>()
        );

        return services;
    }
}
