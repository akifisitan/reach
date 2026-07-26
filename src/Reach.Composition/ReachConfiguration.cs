namespace Reach.Composition;

public static class ReachConfiguration
{
    public static IConfigurationBuilder ConfigureReachConfiguration(
        this IConfigurationBuilder configuration,
        string appDataBasePath,
        bool includeAppDataConfig
    )
    {
        configuration.Sources.Clear();
        configuration.AddJsonFile("config.json", optional: true, reloadOnChange: false);

#if DEBUG
        configuration.AddJsonFile("config.debug.json", optional: true, reloadOnChange: false);
#endif

        if (includeAppDataConfig)
        {
            configuration.AddJsonFile(
                Path.Combine(appDataBasePath, "config.json"),
                optional: true,
                reloadOnChange: false
            );
            configuration.AddEnvironmentVariables(prefix: "REACH_");
        }

        return configuration;
    }
}
