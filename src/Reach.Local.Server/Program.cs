using Microsoft.Extensions.Hosting;
using Reach.Local.Server;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("config.json", optional: true, reloadOnChange: false);

#if DEBUG
builder.Configuration.AddJsonFile("config.debug.json", optional: true, reloadOnChange: false);
#endif

builder.Services.AddApplication(builder.Configuration);

builder.Services.AddHostedService<LocalServerHostedService>();

using var host = builder.Build();

host.Run();
