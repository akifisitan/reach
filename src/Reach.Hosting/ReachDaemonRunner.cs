using Microsoft.Extensions.Hosting;
using Reach.Infra.Persistence;
using Reach.Module;
using Reach.Relay;

namespace Reach.Hosting;

public static class ReachDaemonRunner
{
    public static async Task<int> Run(
        string[] args,
        ReachAppPaths paths,
        Action<IServiceCollection, IConfiguration, ReachAppPaths> configureServices
    )
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Configuration.ConfigureReachConfiguration(paths.AppDataBasePath);

        configureServices(builder.Services, builder.Configuration, paths);

        using var host = builder.Build();

        var applicationLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

        applicationLifetime.ApplicationStarted.Register(() =>
            Console.WriteLine("Started application")
        );
        applicationLifetime.ApplicationStopping.Register(() =>
            Console.WriteLine("Stopping application")
        );

        var logger = host
            .Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(ReachDaemonRunner));

        if (paths.LockFilePath is not null)
        {
            var appLock = host.Services.GetRequiredService<ISingleAppInstanceLock>();

            if (!appLock.TryAcquire(paths.LockFilePath))
            {
                const string message = "There's already a running instance.";

                logger.ZLogError($"{message}");

                await Console.Error.WriteLineAsync(message);

                return 2;
            }
        }

        try
        {
            host.Services.ValidateMessageGraph();

            var setupCoordinator = host.Services.GetRequiredService<RelaySetupCoordinator>();

            await setupCoordinator.EnsureSetup();

            host.Services.GetRequiredService<DatabaseInitializer>().Initialize();

            await RunStartupChecks(host.Services);

            await host.RunAsync();

            return 0;
        }
        catch (RelaySetupRequiredException ex)
        {
            logger.ZLogError($"Relay setup required: {ex.Message}");

            try
            {
                var setupCoordinator = host.Services.GetRequiredService<RelaySetupCoordinator>();

                await setupCoordinator.RunSetup();

                await RunStartupChecks(host.Services);

                await host.RunAsync();

                return 0;
            }
            catch (Exception setupEx)
            {
                Console.WriteLine($"Setup failed: {setupEx.Message}");

                return 1;
            }
        }
        catch (OptionsValidationException ex)
        {
            logger.ZLogError($"Configuration error: {ex.Message}");

            return 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Setup failed: {ex.Message}");

            return 1;
        }
    }

    private static async Task RunStartupChecks(IServiceProvider services)
    {
        foreach (var startupCheck in services.GetServices<IStartupCheck>())
        {
            await startupCheck.Check();
        }
    }
}
