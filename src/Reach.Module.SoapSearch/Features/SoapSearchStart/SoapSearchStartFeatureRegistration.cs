using Microsoft.Extensions.DependencyInjection;

namespace Reach.Module.SoapSearch.Features.SoapSearchStart;

internal static class SoapSearchStartFeatureRegistration
{
    public static IServiceCollection AddSoapSearchStartFeature(this IServiceCollection services)
    {
        services.AddFeature(feature =>
            feature
                .WithChildCliCommand<SoapSearchStartCliCommand>()
                .WithCliCommandHandler<SoapSearchStartCliCommandHandler>()
                .WithJson<SoapSearchStartJsonRegistration>()
                .WithRemoteCommandRpcHandler<SoapSearchStartHandler>()
                .WithRelayCommandHandler<SoapSearchCommandHandler>()
                .WithRelayCommandResultHandler<SoapSearchCommandResultHandler>()
        );
        return services;
    }
}
