using Reach.Plugin.Abstractions;

namespace Reach.Plugin.Example.Features.Echo;

internal static class EchoFeatureRegistration
{
    public static IServiceCollection AddEchoFeature(this IServiceCollection services)
    {
        services.AddPluginFeature(feature =>
            feature
                .WithCliCommand<EchoCliCommand>()
                .WithJson<EchoJsonRegistration>()
                .WithJson<EchoPollJsonRegistration>()
                .WithCliCommandHandler<EchoCliCommandHandler>()
                .WithRpcHandler<EchoRpcHandler>()
                .WithRpcHandler<EchoPollHandler>()
                .WithRelayCommandHandler<EchoCommandHandler>()
                .WithRelayCommandResultHandler<EchoCommandResultHandler>()
        );

        return services;
    }
}
