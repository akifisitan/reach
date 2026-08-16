namespace Reach.Module.LogSearch.Features.LogSearchStart;

internal static class LogSearchStartFeatureRegistration
{
    public static IServiceCollection AddLogSearchStartFeature(this IServiceCollection services)
    {
        services.AddFeature(feature =>
            feature
                .WithCliCommand<SearchStartCliCommand>()
                .WithJson<LogSearchStartJsonRegistration>()
                .WithCliCommandHandler<SearchStartCliCommandHandler>()
                .WithRemoteCommandRpcHandler<LogSearchStartHandler>()
                .WithRelayCommandHandler<SearchFileCommandHandler>()
                .WithRelayCommandResultHandler<SearchFileCommandResultHandler>()
        );

        return services;
    }
}
