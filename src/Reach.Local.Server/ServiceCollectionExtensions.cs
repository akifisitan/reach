using Polly;
using Polly.Retry;
using Reach.Hosting;
using Reach.Infra.DurableQueue;
using Reach.Infra.Persistence;
using Reach.Options;
using Reach.Relay;

namespace Reach.Local.Server;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration,
        ReachAppPaths paths
    )
    {
        services.AddHosting(configuration, paths.AppDataBasePath);

        services.AddReachLogging(configuration, paths);

        services.AddAppOptions(configuration, paths.AppDataBasePath);

        services.AddDurableQueue<RelayCommandResult>(
            x => x.CommandId,
            x =>
            {
                x.MaxTryCount = 4;
                x.TableName = "relay_command_result_queue";
            }
        );

        services.AddResilience(configuration);

        services.AddSingleton<RpcTarget>();
        services.AddSingleton<RemoteCommandCoordinator>();
        services.AddSingleton<LocalServer>();
        services.AddSingleton<LocalPipeRpcServer>();
        services.AddSingleton<RelayCommandResultQueueProcessor>();
        services.AddSingleton<RemoteRelayPoller>();
        services.AddHostedService<LocalServerHostedService>();

        return services;
    }

    private static IServiceCollection AddResilience(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var chaosOptions =
            configuration.GetSection(ChaosOptions.SectionName).Get<ChaosOptions>() ?? new();

        services.AddResiliencePipeline(
            RetryPolicyNames.Default,
            x =>
            {
                x.AddRetry(
                    new RetryStrategyOptions
                    {
                        UseJitter = true,
                        MaxRetryAttempts = 3,
                        Delay = TimeSpan.FromMilliseconds(500),
                        ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                            ex switch
                            {
                                AlreadyQueuedException => false,
                                OperationCanceledException => false,
                                _ => true,
                            }
                        ),
                    }
                );

                x.AddConfiguredChaosFault(chaosOptions, chaosOptions.Retry, "retry");
            }
        );

        services.AddResiliencePipeline(
            ChaosPolicyNames.RpcTransport,
            x => x.AddConfiguredChaosFault(chaosOptions, chaosOptions.RpcTransport, "rpc-transport")
        );

        services.AddResiliencePipeline(
            ChaosPolicyNames.RelayResult,
            x => x.AddConfiguredChaosFault(chaosOptions, chaosOptions.RelayResult, "relay-result")
        );

        services.AddResiliencePipeline(
            ChaosPolicyNames.RelayPost,
            x => x.AddConfiguredChaosFault(chaosOptions, chaosOptions.RelayPost, "relay-post")
        );

        services.AddResiliencePipeline(
            ChaosPolicyNames.RelayPoll,
            x =>
            {
                x.AddConfiguredChaosLatency(chaosOptions, chaosOptions.RelayPoll)
                    .AddConfiguredChaosFault(chaosOptions, chaosOptions.RelayPoll, "relay-poll");
            }
        );

        return services;
    }

    private static IServiceCollection AddAppOptions(
        this IServiceCollection services,
        IConfiguration configuration,
        string basePath
    )
    {
        services.AddOptions<AppOptions>().Bind(configuration.GetSection(nameof(AppOptions)));

        services
            .AddOptions<ChaosOptions>()
            .Bind(configuration.GetSection(ChaosOptions.SectionName))
            .WithValidator<ChaosOptions, ChaosOptionsValidator>();

        services
            .AddOptions<DbConnectionFactoryOptions>()
            .Configure(x =>
            {
                x.DbPath = Path.Combine(basePath, "db", "reach.db");
            });

        services
            .AddOptions<ServerExecutorOptions>()
            .Bind(configuration.GetSection(ServerExecutorOptions.SectionName))
            .WithValidator<ServerExecutorOptions, ServerExecutorOptionsValidator>();

        return services;
    }
}
