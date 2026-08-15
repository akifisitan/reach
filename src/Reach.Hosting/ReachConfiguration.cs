namespace Reach.Hosting;

public static class ReachConfiguration
{
    public static IConfigurationBuilder ConfigureReachConfiguration(
        this IConfigurationBuilder configuration,
        string appDataBasePath
    )
    {
        configuration.Sources.Clear();

        Directory.CreateDirectory(appDataBasePath);

        configuration.AddJsonFile(
            Path.Combine(appDataBasePath, "config.json"),
            optional: true,
            reloadOnChange: false
        );

#if DEBUG
        configuration.AddJsonFile("config.json", optional: true, reloadOnChange: false);
#endif
        return configuration;
    }
}
