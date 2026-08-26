using Microsoft.Extensions.Hosting;
using Reach.Infra.Persistence;
using Reach.Module;
using Reach.Options;
using Reach.Relay;

namespace Reach.Hosting;

public static class ReachDaemonRunner
{
    public static async Task<int> Run(
        string[] args,
        ReachAppPaths paths,
        string relaySetupCommand,
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
            Directory.CreateDirectory(Path.GetDirectoryName(paths.LockFilePath)!);

            var appLock = host.Services.GetRequiredService<ISingleAppInstanceLock>();

            if (!appLock.TryAcquire(paths.LockFilePath))
            {
                const string message = "There's already a running instance.";

                logger.ZLogError($"{message}");
                await Console.Error.WriteLineAsync(message).ConfigureAwait(false);
                return 2;
            }
        }

        try
        {
            ValidateRelayRegistration(host.Services);
            ValidateOptions(host.Services);
            host.Services.ValidateMessageGraph();

            await RunStartupChecks(host.Services, applicationLifetime.ApplicationStopping)
                .ConfigureAwait(false);

            host.Services.GetRequiredService<DatabaseInitializer>().Initialize();
            await host.RunAsync().ConfigureAwait(false);

            return 0;
        }
        catch (RelayConfigurationException ex)
        {
            logger.ZLogError(ex, $"Relay configuration is invalid");
            await Console
                .Error.WriteLineAsync(
                    $"Relay configuration is invalid: {ex.Message}{Environment.NewLine}Run `{relaySetupCommand}`."
                )
                .ConfigureAwait(false);
            return 3;
        }
        catch (RelaySetupRequiredException ex)
        {
            logger.ZLogError(ex, $"Relay setup is required");
            await Console
                .Error.WriteLineAsync(
                    $"Relay setup is required: {ex.Message}{Environment.NewLine}Run `{relaySetupCommand}`."
                )
                .ConfigureAwait(false);
            return 4;
        }
        catch (RelayUnavailableException ex)
        {
            logger.ZLogError(ex, $"Relay is unavailable");
            await Console
                .Error.WriteLineAsync($"Relay is unavailable: {ex.Message}")
                .ConfigureAwait(false);
            return 5;
        }
        catch (AggregateOptionsValidationException ex)
        {
            logger.ZLogError($"Configuration error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            logger.ZLogError(ex, $"Application startup failed");
            await Console
                .Error.WriteLineAsync($"Application startup failed: {ex.Message}")
                .ConfigureAwait(false);
            return 1;
        }
    }

    private static async Task RunStartupChecks(
        IServiceProvider services,
        CancellationToken cancellationToken
    )
    {
        foreach (var startupCheck in services.GetServices<IStartupCheck>())
        {
            await startupCheck.Check(cancellationToken).ConfigureAwait(false);
        }
    }

    private static void ValidateRelayRegistration(IServiceProvider services)
    {
        var registrations = services.GetServices<IRelayRegistration>().ToList();

        if (registrations.Count != 1)
        {
            throw new InvalidOperationException(
                $"Expected exactly one relay registration, but found {registrations.Count}."
            );
        }
    }

    private static void ValidateOptions(IServiceProvider services)
    {
        var failures = services
            .GetServices<IOptionsValidationTarget>()
            .Select(x => x.Validate())
            .OfType<OptionsValidationFailure>()
            .ToList();

        if (failures.Count > 0)
        {
            throw new AggregateOptionsValidationException(failures);
        }
    }
}
