using Reach.Module.Example.LogSearch.Features.LogSearchStart;
using Reach.Module.Example.LogSearch.Features.LogSearchStatus;
using Reach.Module.Toolkit.ResultStore.Sqlite;

namespace Reach.Module.Example.LogSearch;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExampleLogSearchModule(this IServiceCollection services)
    {
        services.AddSingleton<FileSearcher>();
        services.AddSqliteResultStore<SearchFileCommandResult>(
            "log_search_results",
            TimeSpan.FromHours(24)
        );

        services.AddRootCliCommand<LogSearchCliCommand>();

        services.AddLogSearchStartFeature();
        services.AddLogSearchStatusFeature();

        return services;
    }
}
