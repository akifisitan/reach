namespace Reach.Module.Example.LogSearch.Features.LogSearchStatus;

internal static class LogSearchStatusFeatureRegistration
{
    public static IServiceCollection AddLogSearchStatusFeature(this IServiceCollection services)
    {
        services.AddFeature(feature =>
            feature
                .WithChildCliCommand<LogSearchStatusCliCommand>()
                .WithJson<LogSearchStatusJsonRegistration>()
                .WithCliCommandHandler<LogSearchStatusCliCommandHandler>()
                .WithRpcHandler<LogSearchStatusHandler>()
        );

        return services;
    }
}
