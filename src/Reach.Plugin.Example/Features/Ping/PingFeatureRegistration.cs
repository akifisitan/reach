using Reach.Plugin.Abstractions;

namespace Reach.Plugin.Example.Features.Ping;

internal static class PingFeatureRegistration
{
    public static IServiceCollection AddPingFeature(this IServiceCollection services)
    {
        services.AddPluginFeature(feature =>
            feature
                .WithCliCommand<PingCliCommand>()
                .WithJson<PingJsonRegistration>()
                .WithJson<PingPollJsonRegistration>()
                .WithCliCommandHandler<PingCliCommandHandler>()
                .WithRpcHandler<PingRpcHandler>()
                .WithRpcHandler<PingPollHandler>()
                .WithRelayCommandHandler<PingCommandHandler>()
                .WithRelayCommandResultHandler<PingCommandResultHandler>()
        );

        return services;
    }
}
