using Microsoft.Extensions.Hosting;
using Reach.Hosting;
using Reach.Local.Cli;
using Reach.Module;

var paths = ReachAppPaths.LocalCli();
var compositionPaths = ReachAppPaths.LocalServer();

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.ConfigureReachConfiguration(compositionPaths.AppDataBasePath);

builder.Services.AddApplication(builder.Configuration, paths, compositionPaths);

using var host = builder.Build();

host.Services.ValidateMessageGraph();

var cli = host.Services.GetRequiredService<LocalCliExecutor>();

await cli.Execute(args);
