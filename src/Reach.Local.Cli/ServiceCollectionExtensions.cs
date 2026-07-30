using Reach.Hosting;
using Reach.Local.Shared;
using Reach.Module;
using ZLogger.Formatters;

namespace Reach.Local.Cli;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration,
        ReachAppPaths paths,
        ReachAppPaths compositionPaths
    )
    {
        services.AddLocalShared(configuration, compositionPaths.AppDataBasePath);

        services.AddAppLogging(paths);

        services.AddAppOptions(configuration);

        services.AddSingleton<IRpcClient, LocalPipeRpcClient>();
        services.AddSingleton<LocalCliExecutor>();
        services.AddSingleton<RootCliCommand>();
        services.AddSingleton<IConsoleWriter, ConsoleWriter>();

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
                x.ServerBinaryPath = Path.Combine(Environment.CurrentDirectory, "reachld.exe");
            });

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
