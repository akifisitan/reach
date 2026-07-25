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

        services.AddSingleton<ICliCommand, SearchCliCommand>();

        services.AddLogSearchStartFeature();
        services.AddLogSearchStatusFeature();

        return services;
    }
}
