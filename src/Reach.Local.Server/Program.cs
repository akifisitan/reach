using Reach.Hosting;
using Reach.Local.Server;

await ReachDaemonRunner.Run(
    args,
    ReachAppPaths.LocalServer(),
    (services, configuration, paths) => services.AddApplication(configuration, paths)
);
