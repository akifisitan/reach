using Reach.Hosting;

namespace Reach.Local.Server;

public static class LocalServerStartup
{
    public static Task<int> Start(string[] args)
    {
        return ReachDaemonRunner.Run(
            args,
            ReachAppPaths.LocalServer(),
            (services, configuration, paths) => services.AddApplication(configuration, paths)
        );
    }
}
