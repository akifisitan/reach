using Polly;
using Polly.Retry;
using Reach.Hosting;
using Reach.Infra.CommandResultOutbox;
using Reach.Infra.DurableQueue;
using Reach.Infra.Persistence;
using Reach.Relay;

namespace Reach.Remote.Agent;

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

        services.AddDurableQueue<RelayCommand>(
            x => x.Id,
            x =>
            {
                x.MaxTryCount = 4;
                x.TableName = "relay_command_queue";
            }
        );

        services.AddResilience(configuration);

        services.AddSingleton<CommandQueueProcessor>();
        services.AddSingleton<CommandResultPoster>();
        services.AddSingleton<SqliteCommandResultOutboxStore>();
        services.AddSingleton<ICommandResultOutboxStore>(sp =>
            sp.GetRequiredService<SqliteCommandResultOutboxStore>()
        );
        services.AddSingleton<CommandResultOutbox>();
        services.AddSingleton<IDbInitializable>(sp =>
            sp.GetRequiredService<SqliteCommandResultOutboxStore>()
        );
        services.AddSingleton<LocalRelayPoller>();

        services.AddSingleton<RemoteAgent>();

        services.AddHostedService<RemoteAgentHostedService>();

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
            Constants.DefaultRetryPolicy,
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
        services.AddOptions<AppOptions>().Bind(configuration.GetSection(AppOptions.SectionName));

        services
            .AddOptions<ChaosOptions>()
            .Bind(configuration.GetSection(ChaosOptions.SectionName));

        services
            .AddOptions<ExecutorOptions>()
            .Configure(x =>
            {
                x.ProcessorCount = 1;
                x.RetryConsumerCount = 1;
                x.PosterCount = 1;
            });

        services
            .AddOptions<SqliteCommandResultOutboxOptions>()
            .Configure(x =>
            {
                x.TableName = "command_result_outbox";
            });

        services.AddOptions<CommandResultOutboxOptions>();

        services
            .AddOptions<DbConnectionFactoryOptions>()
            .Configure(x =>
            {
                x.DbPath = Path.Combine(basePath, "db", "reach.db");
            });

        return services;
    }
}
