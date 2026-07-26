namespace Reach.Relay.LocalFile;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLocalFileRelay(this IServiceCollection services)
    {
        services.AddSingleton<LocalFileRelay>();
        services.AddSingleton<ILocalRelay>(sp => sp.GetRequiredService<LocalFileRelay>());
        services.AddSingleton<IRemoteRelay>(sp => sp.GetRequiredService<LocalFileRelay>());

        services
            .AddOptions<LocalFileRelayOptions>()
            .Configure(x =>
            {
                var basePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Reach.Debug",
                    "local",
                    "localrelay"
                );

                Directory.CreateDirectory(basePath);

                x.LocalToRemoteFilePath = Path.Combine(basePath, "relay_local_to_remote.jsonl");
                x.RemoteToLocalFilePath = Path.Combine(basePath, "relay_remote_to_local.jsonl");

                File.AppendAllText(x.LocalToRemoteFilePath, string.Empty);
                File.AppendAllText(x.RemoteToLocalFilePath, string.Empty);
            });

        return services;
    }
}
