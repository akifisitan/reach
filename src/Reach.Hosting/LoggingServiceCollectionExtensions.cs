using System.Diagnostics;
using ZLogger.Formatters;

namespace Reach.Hosting;

public static class LoggingServiceCollectionExtensions
{
    public static IServiceCollection AddReachLogging(
        this IServiceCollection services,
        IConfiguration configuration,
        ReachAppPaths paths
    )
    {
        services.AddLogging(x =>
        {
            x.ClearProviders()
                .AddConfiguration(configuration.GetSection("Logging"))
                .AddZLoggerFile(
                    paths.LogFilePath,
                    options =>
                    {
                        options.UsePlainTextFormatter(
                            static (PlainTextZLoggerFormatter formatter) =>
                            {
                                formatter.SetPrefixFormatter(
                                    $"[{0:local-longdate}][{1:short}][{2}] ",
                                    (in MessageTemplate template, in LogInfo logInfo) =>
                                        template.Format(
                                            logInfo.Timestamp,
                                            logInfo.LogLevel,
                                            logInfo.Category
                                        )
                                );
                            }
                        );
                    }
                );
        });

        OpenLogFileOnStart(configuration, paths);

        return services;
    }

    private static void OpenLogFileOnStart(IConfiguration configuration, ReachAppPaths paths)
    {
        var options = configuration
            .GetSection(nameof(ReachLoggingOptions))
            .Get<ReachLoggingOptions>();

        if (options?.OpenLogFileOnStart != true)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(paths.LogFilePath)!);

            if (!File.Exists(paths.LogFilePath))
            {
                File.WriteAllText(paths.LogFilePath, string.Empty);
            }

            using var _ = Process.Start(
                new ProcessStartInfo { FileName = paths.LogFilePath, UseShellExecute = true }
            );
        }
        catch
        {
            // Opening the log file is a debugging convenience and must not block startup.
        }
    }
}
