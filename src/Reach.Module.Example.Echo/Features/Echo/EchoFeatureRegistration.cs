namespace Reach.Module.Example.Echo.Features.Echo;

internal static class EchoFeatureRegistration
{
    public static IServiceCollection AddEchoFeature(this IServiceCollection services)
    {
        services.AddFeature(feature =>
            feature
                .WithRootCliCommand<EchoCliCommand>()
                .WithJson<EchoJsonRegistration>()
                .WithJson<EchoPollJsonRegistration>()
                .WithCliCommandHandler<EchoCliCommandHandler>()
                .WithRemoteCommandRpcHandler<EchoRpcHandler>()
                .WithRpcHandler<EchoPollHandler>()
                .WithRelayCommandHandler<EchoCommandHandler>()
                .WithRelayCommandResultHandler<EchoCommandResultHandler>()
        );

        return services;
    }
}
