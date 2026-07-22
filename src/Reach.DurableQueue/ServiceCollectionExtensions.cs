using Polly;
using Polly.Retry;
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

        services.AddResiliencePipeline(
            DurableQueueConstants.DefaultRetryPolicy,
            x =>
            {
                x.AddRetry(
                    new RetryStrategyOptions
                    {
                        UseJitter = true,
                        MaxRetryAttempts = 3,
                        Delay = TimeSpan.FromMilliseconds(500),
                    }
                );
            }
        );

        return services;
    }
}
