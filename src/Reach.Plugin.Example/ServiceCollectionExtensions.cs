using Reach.Plugin.Abstractions;
using Reach.Plugin.Example.Features.Echo;

namespace Reach.Plugin.Example;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExamplePlugin(this IServiceCollection services)
    {
        services.AddSingleton<EchoResultStore>();

        services.AddSingleton<ICliCommand, ExampleCliCommand>();

        services.AddEchoFeature();

        return services;
    }
}
