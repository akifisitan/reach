using Reach.Module.LogSearch.Features.LogSearchStart;
using Reach.Module.LogSearch.Features.LogSearchStatus;
using Reach.Module.Toolkit.ResultStore.Sqlite;

namespace Reach.Module.LogSearch;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLogSearchModule(this IServiceCollection services)
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
