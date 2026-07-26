using Polly;
using Polly.Retry;
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
        IConfiguration configuration
    )
    {
        services.AddLocalShared(configuration, AppPaths.AppDataLocalBasePath);

        services.AddAppLogging();

        services.AddAppOptions(configuration);

        services.AddDurableQueue<RelayCommandResult>(
            x => x.Id,
            x =>
            {
                x.MaxTryCount = 4;
                x.TableName = "relay_command_result_queue";
            }
        );

        services.AddResiliencePipeline(
            AppConstants.DefaultRetryPolicy,
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
            }
        );

        services
            .AddOptions<DbConnectionFactoryOptions>()
            .Configure(x =>
            {
                x.DbPath = Path.Combine(AppPaths.AppDataLocalBasePath, "db", "reach.db");
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

        return services;
    }

    private static IServiceCollection AddAppLogging(
        this IServiceCollection services,
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
                    AppPaths.LogFilePath,
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
