using Reach.Plugin.LogSearch.Features.LogSearchStart;
using Reach.Plugin.LogSearch.Features.LogSearchStatus;
using Reach.Plugin.Toolkit.ResultStore.Sqlite;

namespace Reach.Plugin.LogSearch;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLogSearchPlugin(this IServiceCollection services)
    {
        services.AddSingleton<FileSearcher>();
        services.AddSqliteResultStore<SearchFileCommandResult>(
            "log_search_results",
            TimeSpan.FromHours(24)
        );

        services.AddSingleton<ICliCommand, SearchCliCommand>();

        services.AddLogSearchStartFeature();
        services.AddLogSearchStatusFeature();

        return services;
    }
}
