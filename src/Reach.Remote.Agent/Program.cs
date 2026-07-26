using Microsoft.Extensions.Hosting;
using Reach.Composition;
using Reach.Infra.Persistence;
using Reach.Plugin;
using Reach.Relay;
using Reach.Remote.Agent;

Directory.CreateDirectory(AppPaths.AppDataLocalBasePath);

var setupCoordinator = new RelaySetupCoordinator(AppPaths.AppDataLocalBasePath);

while (true)
{
    try
    {
        await setupCoordinator.EnsureSetupAsync();

        break;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Setup failed: {ex.Message}");

        Environment.ExitCode = 1;

        return;
    }
}

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("config.json", optional: true, reloadOnChange: false);

#if DEBUG
builder.Configuration.AddJsonFile("config.debug.json", optional: true, reloadOnChange: false);
#endif

builder.Configuration.AddJsonFile(
    Path.Combine(AppPaths.AppDataLocalBasePath, "config.json"),
    optional: true,
    reloadOnChange: false
);
builder.Configuration.AddEnvironmentVariables(prefix: "REACH_");

builder.Services.AddApplication(builder.Configuration);

using var host = builder.Build();

var appLock = host.Services.GetRequiredService<ISingleAppInstanceLock>();

if (!appLock.TryAcquire(AppPaths.LockFilePath))
{
    host.Services.GetRequiredService<ILoggerFactory>()
        .CreateLogger("Program")
        .ZLogInformation($"There's already a running instance, exiting...");

    return;
}

host.Services.ValidatePluginGraph();

host.Services.GetRequiredService<DatabaseInitializer>().Initialize();

try
{
    await host.RunAsync();
}
catch (RelaySetupRequiredException ex)
{
    host.Services.GetRequiredService<ILoggerFactory>()
        .CreateLogger("Program")
        .ZLogError($"Relay setup required: {ex.Message}");

    try
    {
        await setupCoordinator.RunSetupAsync();
    }
    catch (Exception setupEx)
    {
        Console.WriteLine($"Setup failed: {setupEx.Message}");

        Environment.ExitCode = 1;
    }
}
catch (Microsoft.Extensions.Options.OptionsValidationException ex)
{
    host.Services.GetRequiredService<ILoggerFactory>()
        .CreateLogger("Program")
        .ZLogError($"Configuration error: {ex.Message}");

    Environment.ExitCode = 1;
}
