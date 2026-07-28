namespace Reach.Module.Example.Features.Echo;

internal static class EchoFeatureRegistration
{
    public static IServiceCollection AddEchoFeature(this IServiceCollection services)
    {
        services.AddFeature(feature =>
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
