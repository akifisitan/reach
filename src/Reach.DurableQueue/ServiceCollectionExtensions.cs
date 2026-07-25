using Reach.Persistence;
using Reach.Relay.Abstractions;

namespace Reach.DurableQueue;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDurableQueue<T>(
        this IServiceCollection services,
        Func<T, string> idSelector,
        Action<DurableQueueOptions> configure
    )
    {
        services.AddSingleton<IDbInitializable>(sp =>
            sp.GetRequiredService<IDurableQueueRepository>()
        );
        services.AddSingleton<IDurableQueueRepository, DurableQueueRepository>();
        services.AddOptions<DurableQueueOptions>().Configure(configure);

        services.AddSingleton<IDurableQueue<T>>(sp => new DurableQueue<T>(
            sp.GetRequiredService<IRelayPayloadSerializer>(),
            sp.GetRequiredService<IDurableQueueRepository>(),
            idSelector
        ));

        return services;
    }
}
