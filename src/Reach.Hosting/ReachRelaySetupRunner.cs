using Microsoft.Extensions.Hosting;
using Reach.Relay;

namespace Reach.Hosting;

public static class ReachRelaySetupRunner
{
    public static async Task<int> Run(
        string[] args,
        ReachAppPaths paths,
        Action<IServiceCollection, IConfiguration, ReachAppPaths> configureServices
    )
    {
        if (Console.IsInputRedirected)
        {
            await Console
                .Error.WriteLineAsync("Relay setup requires an interactive terminal.")
                .ConfigureAwait(false);
            return 1;
        }

        var builder = Host.CreateApplicationBuilder(args);

        builder.Configuration.ConfigureReachConfiguration(paths.AppDataBasePath);
        configureServices(builder.Services, builder.Configuration, paths);

        using var host = builder.Build();

        var registrations = host.Services.GetServices<IRelayRegistration>().ToList();

        if (registrations.Count != 1)
        {
            await Console
                .Error.WriteLineAsync(
                    $"Expected exactly one relay registration, but found {registrations.Count}."
                )
                .ConfigureAwait(false);
            return 1;
        }

        var setup = host.Services.GetServices<IRelaySetup>().SingleOrDefault();

        if (setup is null || setup.Name != registrations[0].Name)
        {
            await Console
                .Error.WriteLineAsync(
                    $"The active relay '{registrations[0].Name}' does not provide setup."
                )
                .ConfigureAwait(false);
            return 1;
        }

        if (paths.LockFilePath is not null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(paths.LockFilePath)!);

            var appLock = host.Services.GetRequiredService<ISingleAppInstanceLock>();

            if (!appLock.TryAcquire(paths.LockFilePath))
            {
                await Console
                    .Error.WriteLineAsync("Setup cannot run while the application is running.")
                    .ConfigureAwait(false);
                return 2;
            }
        }

        try
        {
            return await setup.Run(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Setup failed: {ex.Message}").ConfigureAwait(false);
            return 1;
        }
    }
}
