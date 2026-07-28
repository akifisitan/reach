namespace Reach.Module.LogSearch.Features.LogSearchStatus;

internal static class LogSearchStatusFeatureRegistration
{
    public static IServiceCollection AddLogSearchStatusFeature(this IServiceCollection services)
    {
        services.AddFeature(feature =>
            feature
                .WithCliCommand<SearchStatusCliCommand>()
                .WithJson<LogSearchStatusJsonRegistration>()
                .WithCliCommandHandler<SearchStatusCliCommandHandler>()
                .WithRpcHandler<LogSearchStatusHandler>()
        );

        return services;
    }
}
