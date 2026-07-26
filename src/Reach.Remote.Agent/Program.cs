using Reach.Composition;
using Reach.Remote.Agent;

await ReachDaemonRunner.Run(
    args,
    ReachAppPaths.RemoteAgent(),
    (services, configuration, paths) => services.AddApplication(configuration, paths)
);
