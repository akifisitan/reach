using Reach.Local.Cli;
using Reach.Local.Server;

if (args is ["setup", "relay", .. var setupArgs])
{
    return await LocalServerStartup.SetupRelay(setupArgs);
}

if (args is ["serve", .. var serveArgs])
{
    return await LocalServerStartup.Start(serveArgs);
}

return await LocalCliStartup.Start(args);
