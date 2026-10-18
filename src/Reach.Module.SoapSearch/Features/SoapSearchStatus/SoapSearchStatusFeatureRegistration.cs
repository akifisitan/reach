using Microsoft.Extensions.DependencyInjection;

namespace Reach.Module.SoapSearch.Features.SoapSearchStatus;

internal static class SoapSearchStatusFeatureRegistration
{
    public static IServiceCollection AddSoapSearchStatusFeature(this IServiceCollection services)
    {
        services.AddFeature(feature =>
            feature
                .WithChildCliCommand<SoapSearchStatusCliCommand>()
                .WithCliCommandHandler<SoapSearchStatusCliCommandHandler>()
                .WithJson<SoapSearchStatusJsonRegistration>()
                .WithRpcHandler<SoapSearchStatusHandler>()
        );
        return services;
    }
}
