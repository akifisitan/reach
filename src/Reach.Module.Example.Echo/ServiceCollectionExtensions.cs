using Reach.Module.Example.Echo.Features.Echo;
using Reach.Module.Toolkit.ResultStore.Memory;

namespace Reach.Module.Example.Echo;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExampleEchoModule(this IServiceCollection services)
    {
        services.AddSingleton<IEchoFormatter, EchoFormatter>();
        services.AddMemoryResultStore<EchoCommandResult>();

        services.AddEchoFeature();

        return services;
    }
}
