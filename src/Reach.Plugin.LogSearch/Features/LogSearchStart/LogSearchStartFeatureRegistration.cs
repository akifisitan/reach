using Reach.Plugin.Abstractions;

namespace Reach.Plugin.LogSearch.Features.LogSearchStart;

internal static class LogSearchStartFeatureRegistration
{
    public static IServiceCollection AddLogSearchStartFeature(this IServiceCollection services)
    {
        services.AddPluginFeature(feature =>
            feature
                .WithCliCommand<SearchStartCliCommand>()
                .WithJson<LogSearchStartJsonRegistration>()
                .WithCliCommandHandler<SearchStartCliCommandHandler>()
                .WithRpcHandler<LogSearchStartHandler>()
                .WithRelayCommandHandler<SearchFileCommandHandler>()
                .WithRelayCommandResultHandler<SearchFileCommandResultHandler>()
        );

        return services;
    }
}
