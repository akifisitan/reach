using Reach.Infra.Persistence;
using Reach.Options;

namespace Reach.Infra.DurableQueue;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDurableQueue<T>(
        this IServiceCollection services,
        Func<T, string> idSelector,
        Action<DurableQueueOptions> configure
    )
    {
        if (services.Any(x => x.ServiceType == typeof(IDurableQueue<T>)))
        {
            throw new InvalidOperationException(
                "Only one durable queue per process is currently supported."
            );
        }

        services.AddSingleton<IDbInitializable>(sp =>
            sp.GetRequiredService<IDurableQueueRepository>()
        );
        services.AddSingleton<IDurableQueueRepository, DurableQueueRepository>();
        services
            .AddOptions<DurableQueueOptions>()
            .Configure(configure)
            .WithValidator<DurableQueueOptions, DurableQueueOptionsValidator>();

        services.AddSingleton(sp => new DurableQueue<T>(
            sp.GetRequiredService<IDurableQueueDataSerializer>(),
            sp.GetRequiredService<IDurableQueueRepository>(),
            idSelector
        ));
        services.AddSingleton<IDurableQueue<T>>(sp => sp.GetRequiredService<DurableQueue<T>>());

        return services;
    }
}
