using Polly;

namespace Reach.Relay.Confluence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConfluenceRelay(this IServiceCollection services)
    {
        services.AddSingleton<ILocalRelay, ConfluenceCommentRelay>();
        services.AddSingleton<IRemoteRelay, ConfluenceCommentRelay>();
        services.AddSingleton<IConfluenceHttpClientFactory, ConfluenceHttpClientFactory>();

        services.AddResiliencePipeline(
            nameof(ConfluenceCommentRelay),
            x =>
            {
                //x.AddRetry(new RetryStrategyOptions
                //{
                //    UseJitter = true,
                //    MaxRetryAttempts = 2,
                //    ShouldHandle = (x) => ValueTask.FromResult(false)
                //});
            }
        );

        return services;
    }
}
