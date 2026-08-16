namespace Reach.Module.Example.LogSearch.Features.LogSearchStart;

internal static class LogSearchStartFeatureRegistration
{
    public static IServiceCollection AddLogSearchStartFeature(this IServiceCollection services)
    {
        services.AddFeature(feature =>
            feature
                .WithCliCommand<LogSearchStartCliCommand>()
                .WithJson<LogSearchStartJsonRegistration>()
                .WithCliCommandHandler<LogSearchStartCliCommandHandler>()
                .WithRemoteCommandRpcHandler<LogSearchStartHandler>()
                .WithRelayCommandHandler<SearchFileCommandHandler>()
                .WithRelayCommandResultHandler<SearchFileCommandResultHandler>()
        );

        return services;
    }
}
