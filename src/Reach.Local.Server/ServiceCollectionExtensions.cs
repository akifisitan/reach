using Polly;
using Polly.Retry;
using Reach.Hosting;
using Reach.Infra.DurableQueue;
using Reach.Infra.Persistence;
using Reach.Local.Shared;
using Reach.Relay;
using ZLogger.Formatters;

namespace Reach.Local.Server;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration,
        ReachAppPaths paths
    )
    {
        services.AddLocalShared(configuration, paths.AppDataBasePath);

        services.AddAppLogging(paths);

        services.AddAppOptions(configuration);

        var chaosOptions =
            configuration.GetSection(ChaosOptions.SectionName).Get<ChaosOptions>() ?? new();

        services.AddDurableQueue<RelayCommandResult>(
            x => x.Id,
            x =>
            {
                x.MaxTryCount = 4;
                x.TableName = "relay_command_result_queue";
            }
        );

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
            ChaosPolicyNames.RpcTransport,
            x => x.AddConfiguredChaosFault(chaosOptions, chaosOptions.RpcTransport, "rpc-transport")
        );

        services.AddResiliencePipeline(
            ChaosPolicyNames.RelayPost,
            x => x.AddConfiguredChaosFault(chaosOptions, chaosOptions.RelayPost, "relay-post")
        );

        services.AddResiliencePipeline(
            ChaosPolicyNames.RelayResult,
            x => x.AddConfiguredChaosFault(chaosOptions, chaosOptions.RelayResult, "relay-result")
        );

        services.AddResiliencePipeline(
            ChaosPolicyNames.RelayPoll,
            x =>
            {
                x.AddConfiguredChaosLatency(chaosOptions, chaosOptions.RelayPoll)
                    .AddConfiguredChaosFault(chaosOptions, chaosOptions.RelayPoll, "relay-poll");
            }
        );

        services
            .AddOptions<DbConnectionFactoryOptions>()
            .Configure(x =>
            {
                x.DbPath = Path.Combine(paths.AppDataBasePath, "db", "reach.db");
            });

        services.AddSingleton<RpcTarget>();
        services.AddSingleton<LocalServer>();
        services.AddSingleton<LocalPipeRpcServer>();
        services.AddSingleton<EventQueueProcessor>();
        services.AddSingleton<RemoteRelayPoller>();
        services
            .AddOptions<ServerExecutorOptions>()
            .Configure(x =>
            {
                x.ConsumerCount = 1;
                x.RetryConsumerCount = 1;
            });

        services.AddHostedService<LocalServerHostedService>();

        return services;
    }

    private static IServiceCollection AddAppOptions(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<AppOptions>()
            .Bind(configuration.GetRequiredSection(nameof(AppOptions)));

        services
            .AddOptions<ChaosOptions>()
            .Bind(configuration.GetSection(ChaosOptions.SectionName));

        return services;
    }

    private static IServiceCollection AddAppLogging(
        this IServiceCollection services,
        ReachAppPaths paths,
        LogLevel minimumLogLevel = LogLevel.Debug
    )
    {
        services.AddLogging(x =>
        {
            x.ClearProviders()
                .SetMinimumLevel(minimumLogLevel)
                .AddZLoggerConsole(options =>
                {
                    options.UsePlainTextFormatter(ConfigureFormatter);
                })
                .AddZLoggerFile(
                    paths.LogFilePath,
                    options =>
                    {
                        options.UsePlainTextFormatter(ConfigureFormatter);
                    }
                );

            static void ConfigureFormatter(PlainTextZLoggerFormatter formatter)
            {
                formatter.SetPrefixFormatter(
                    $"[{0:local-longdate}][{1:short}][{2}] ",
                    (in MessageTemplate template, in LogInfo logInfo) =>
                        template.Format(logInfo.Timestamp, logInfo.LogLevel, logInfo.Category)
                );
            }
        });

        return services;
    }
}
