using Reach.Plugin.Abstractions;

namespace Reach.Plugin.LogSearch.Features.LogSearchStatus;

internal static class LogSearchStatusFeatureRegistration
{
    public static IServiceCollection AddLogSearchStatusFeature(this IServiceCollection services)
    {
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
