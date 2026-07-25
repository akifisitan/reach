using Microsoft.Extensions.Hosting;
using Reach.Local.Server;
using Reach.Persistence;
using Reach.Plugin.Abstractions;
using Reach.Shared;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("config.json", optional: true, reloadOnChange: false);

#if DEBUG
builder.Configuration.AddJsonFile("config.debug.json", optional: true, reloadOnChange: false);
#endif

builder.Services.AddApplication(builder.Configuration);

builder.Services.AddHostedService<LocalServerHostedService>();

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

host.Run();
