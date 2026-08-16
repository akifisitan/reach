using Microsoft.Extensions.Hosting;
using Reach.Hosting;
using Reach.Module;

namespace Reach.Local.Cli;

public static class LocalCliStartup
{
    public static Task<int> Start(string[] args)
    {
        var paths = ReachAppPaths.LocalCli();

        var compositionPaths = ReachAppPaths.LocalServer();

        var builder = Host.CreateApplicationBuilder(args);

        builder.Configuration.ConfigureReachConfiguration(compositionPaths.AppDataBasePath);

        builder.Services.AddApplication(builder.Configuration, paths, compositionPaths);

        using var host = builder.Build();

        host.Services.ValidateMessageGraph();

        return host.Services.GetRequiredService<LocalCliExecutor>().Execute(args);
    }
}
