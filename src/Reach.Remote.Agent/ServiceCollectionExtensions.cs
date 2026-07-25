using Polly;
using Polly.Retry;
using Reach.DurableQueue;
using Reach.Persistence;
using Reach.Relay.Abstractions;
using Reach.Shared;
using ZLogger.Formatters;

namespace Reach.Remote.Agent;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddShared();

        services.AddAppLogging();

        services.AddSingleton<RemoteAgent>();

        services.AddAppOptions(configuration);

        services.AddDurableQueue<RelayCommand>(
            x => x.Id,
            x =>
            {
                x.MaxTryCount = 4;
                x.TableName = "relay_command_queue";
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

        services.AddSingleton<CommandQueueProcessor>();
        services.AddSingleton<LocalRelayPoller>();

        services.AddHostedService<RemoteAgentHostedService>();

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
            .AddOptions<ExecutorOptions>()
            .Configure(x =>
            {
                x.ConsumerCount = 1;
                x.RetryConsumerCount = 1;
            });

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
                    options.UsePlainTextFormatter(
                        static (PlainTextZLoggerFormatter formatter) =>
                        {
                            formatter.SetPrefixFormatter(
                                $"[{0:local-longdate}][{1:short}] ",
                                (in MessageTemplate template, in LogInfo logInfo) =>
                                    template.Format(logInfo.Timestamp, logInfo.LogLevel)
                            );
                        }
                    );
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
