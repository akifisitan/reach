using Microsoft.Extensions.Configuration;
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
        services.AddSingleton<RelayCommandStore>();

        services.AddAppOptions(configuration);

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
