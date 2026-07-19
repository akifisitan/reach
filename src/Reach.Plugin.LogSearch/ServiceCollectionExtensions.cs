using Microsoft.Extensions.DependencyInjection;
using Reach.Plugin.Abstractions;
using Reach.Plugin.LogSearch.LogSearchStart;
using Reach.Plugin.LogSearch.LogSearchStatus;

namespace Reach.Plugin.LogSearch;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLogSearchPlugin(this IServiceCollection services)
    {
        services.AddSingleton<FileSearcher>();
        services.AddSingleton<SearchResultStore>();

        services.AddSingleton<ICliCommand, SearchCliCommand>();

        // LogSearchStart
        services.AddPluginFeature(feature =>
            feature
                .WithCliCommand<SearchStartCliCommand>()
                .WithJson<LogSearchStartJsonRegistration>()
                .WithCliCommandHandler<SearchStartCliCommandHandler>()
                .WithRpcHandler<LogSearchStartHandler>()
                .WithRelayCommandHandler<SearchFileCommandHandler>()
                .WithRelayCommandResultHandler<SearchFileCommandResultHandler>()
        );

        // LogSearchStatus
        services.AddPluginFeature(feature =>
            feature
                .WithCliCommand<SearchStatusCliCommand>()
                .WithJson<LogSearchStatusJsonRegistration>()
                .WithCliCommandHandler<SearchStatusCliCommandHandler>()
                .WithRpcHandler<LogSearchStatusHandler>()
        );

        return services;
    }
}
