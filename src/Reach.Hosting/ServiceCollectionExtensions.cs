using System.Diagnostics;
using System.Text.Json;
using Reach.Infra.DurableQueue;
using Reach.Infra.NativeConsole;
using Reach.Infra.Persistence;
using Reach.Module;
using Reach.Module.Example;
using Reach.Module.LogSearch;
using Reach.Relay;
using Reach.Relay.LocalFile;
using ZLogger.Formatters;

namespace Reach.Hosting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHosting(
        this IServiceCollection services,
        IConfiguration configuration,
        string basePath
    )
    {
        services.AddSingleton(sp =>
            WireJsonExtensions.CreateJsonSerializerOptions(
                sp.GetServices<IWireJsonRegistration>().ToList()
            )
        );
        services.AddSingleton(sp => new JsonPayloadSerializer(
            sp.GetRequiredService<JsonSerializerOptions>()
        ));
        services.AddSingleton<IRelayPayloadSerializer>(sp =>
            sp.GetRequiredService<JsonPayloadSerializer>()
        );
        services.AddSingleton<IRpcPayloadSerializer>(sp =>
            sp.GetRequiredService<JsonPayloadSerializer>()
        );
        services.AddSingleton<ISingleAppInstanceLock, SingleAppInstanceLock>();
        services.AddSingleton<IConsoleAttacher, ConsoleAttacher>();
        services.AddSingleton<IRelaySetupCoordinator>(sp => new RelaySetupCoordinator(
            basePath,
            sp.GetRequiredService<IConsoleAttacher>(),
            sp.GetServices<IRelaySetup>()
        ));

        services.AddSingleton<IDurableQueueDataSerializer>(sp =>
            sp.GetRequiredService<JsonPayloadSerializer>()
        );

        //services.AddConfluenceRelay(configuration, ReachAppPaths.SharedPath);
        services.AddLocalFileRelay(configuration, ReachAppPaths.SharedPath);

        services.AddPersistence();

        services.AddLogSearchModule();
        services.AddExampleModule();

        return services;
    }

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
