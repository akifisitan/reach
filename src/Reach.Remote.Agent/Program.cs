using Reach.Hosting;
using Reach.Remote.Agent;

if (args is ["setup", "relay", .. var setupArgs])
{
    return await ReachRelaySetupRunner.Run(
        setupArgs,
        ReachAppPaths.RemoteAgent(),
        (services, configuration, paths) => services.AddApplication(configuration, paths)
    );
}

return await ReachDaemonRunner.Run(
    args,
    ReachAppPaths.RemoteAgent(),
    "reachrd setup relay",
    (services, configuration, paths) => services.AddApplication(configuration, paths)
);
