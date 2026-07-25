using Microsoft.Extensions.DependencyInjection;
using Reach.Plugin.Toolkit.ResultStore.Abstractions;
using Reach.Relay.Abstractions;

namespace Reach.Plugin.Toolkit.ResultStore.Memory;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMemoryResultStore<TResult>(
        this IServiceCollection services,
        TimeSpan? stalenessThreshold = null
    )
        where TResult : RelayCommandResult
    {
        services.AddSingleton(sp => new MemoryResultStore<TResult>(
            stalenessThreshold ?? TimeSpan.MaxValue
        ));
        services.AddSingleton<IResultStore<TResult>>(sp =>
            sp.GetRequiredService<MemoryResultStore<TResult>>()
        );

        return services;
    }
}
