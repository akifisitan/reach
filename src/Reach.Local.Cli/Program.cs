using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Reach.Local.Cli;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("config.json", optional: true, reloadOnChange: false);

#if DEBUG
builder.Configuration.AddJsonFile("config.debug.json", optional: true, reloadOnChange: false);
#endif

builder.Services.AddApplication(builder.Configuration);

using var host = builder.Build();

var cli = host.Services.GetRequiredService<LocalCliExecutor>();

await cli.Execute(args);
