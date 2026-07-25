using Reach.Plugin.Abstractions;
using Reach.Plugin.Example.Features.Ping;

namespace Reach.Plugin.Example;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExamplePlugin(this IServiceCollection services)
    {
        services.AddSingleton<PingResultStore>();

        services.AddSingleton<ICliCommand, ExampleCliCommand>();

        services.AddPingFeature();

        return services;
    }
}
