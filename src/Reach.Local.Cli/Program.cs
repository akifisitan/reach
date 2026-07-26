using Microsoft.Extensions.Hosting;
using Reach.Composition;
using Reach.Local.Cli;
using Reach.Plugin;

var paths = ReachAppPaths.LocalCli();
var compositionPaths = ReachAppPaths.LocalServer();

Directory.CreateDirectory(paths.AppDataBasePath);

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.ConfigureReachConfiguration(
    compositionPaths.AppDataBasePath,
    includeAppDataConfig: true
);

builder.Services.AddApplication(builder.Configuration, paths, compositionPaths);

using var host = builder.Build();

host.Services.ValidatePluginGraph();

var cli = host.Services.GetRequiredService<LocalCliExecutor>();

await cli.Execute(args);
