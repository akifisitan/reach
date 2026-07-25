using Reach.Persistence;
using Reach.Plugin.Abstractions;
using Reach.Plugin.LogSearch.Features.LogSearchStart;
using Reach.Plugin.LogSearch.Features.LogSearchStatus;

namespace Reach.Plugin.LogSearch;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLogSearchPlugin(this IServiceCollection services)
    {
        services.AddSingleton<FileSearcher>();
        services.AddSingleton<SearchResultStore>();
        services.AddSingleton<IDbInitializable>(sp => sp.GetRequiredService<SearchResultStore>());
        services
            .AddOptions<SearchResultStoreOptions>()
            .Configure(x =>
            {
                x.TableName = "log_search_results";
                x.StalenessThreshold = TimeSpan.FromHours(24);
            });

        services.AddSingleton<ICliCommand, SearchCliCommand>();

        services.AddLogSearchStartFeature();
        services.AddLogSearchStatusFeature();

        return services;
    }
}
