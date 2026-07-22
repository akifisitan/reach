using Microsoft.Extensions.Configuration;
using Reach.Local.Shared;
using Reach.Plugin.Abstractions;
using ZLogger.Formatters;

namespace Reach.Local.Cli;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddLocalShared();

        services.AddAppLogging();

        services.AddAppOptions(configuration);

        services.AddSingleton<IRpcClient, LocalPipeRpcClient>();
        services.AddSingleton<LocalCliExecutor>();
        services.AddSingleton<RootCliCommand>();

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
            .AddOptions<LocalPipeRpcClientOptions>()
            .Configure(x =>
            {
                x.ShouldNotBootServer =
                    bool.TryParse(
                        Environment.GetEnvironmentVariable(AppConstants.ShouldNotBootServerEnvVar),
                        out var shouldNotBootServer
                    ) && shouldNotBootServer;
                x.ServerBinaryPath = AppPaths.ServerBinaryPath;
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
