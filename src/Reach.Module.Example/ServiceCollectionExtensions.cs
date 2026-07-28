using Reach.Module.Example.Features.Echo;
using Reach.Module.Toolkit.ResultStore.Memory;

namespace Reach.Module.Example;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExampleModule(this IServiceCollection services)
    {
        services.AddMemoryResultStore<EchoCommandResult>();

        services.AddSingleton<ICliCommand, ExampleCliCommand>();

        services.AddEchoFeature();

        return services;
    }
}
