using Reach.Plugin.Example.Features.Echo;
using Reach.Plugin.Toolkit.ResultStore.Memory;

namespace Reach.Plugin.Example;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExamplePlugin(this IServiceCollection services)
    {
        services.AddMemoryResultStore<EchoCommandResult>();

        services.AddSingleton<ICliCommand, ExampleCliCommand>();

        services.AddEchoFeature();

        return services;
    }
}
