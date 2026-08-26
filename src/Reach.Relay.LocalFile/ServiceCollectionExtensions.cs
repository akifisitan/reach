using Reach.Options;

namespace Reach.Relay.LocalFile;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLocalFileRelay(
        this IServiceCollection services,
        string basePath
    )
    {
        if (services.Any(x => x.ServiceType == typeof(IRelayRegistration)))
        {
            throw new InvalidOperationException("A relay has already been registered.");
        }

        services.AddSingleton<IRelayRegistration>(new RelayRegistration("local-file"));
        services.AddSingleton<LocalFileRelay>();
        services.AddSingleton<ILocalRelay>(sp => sp.GetRequiredService<LocalFileRelay>());
        services.AddSingleton<IRemoteRelay>(sp => sp.GetRequiredService<LocalFileRelay>());

        services
            .AddOptions<LocalFileRelayOptions>()
            .Configure(x =>
            {
                var relayBasePath = Path.Combine(basePath, "localrelay");

                Directory.CreateDirectory(relayBasePath);

                x.LocalToRemoteFilePath = Path.Combine(
                    relayBasePath,
                    "relay_local_to_remote.jsonl"
                );
                x.RemoteToLocalFilePath = Path.Combine(
                    relayBasePath,
                    "relay_remote_to_local.jsonl"
                );
                x.PollDelayMs = 3000;

                File.AppendAllText(x.LocalToRemoteFilePath, string.Empty);
                File.AppendAllText(x.RemoteToLocalFilePath, string.Empty);
            })
            .WithValidator<LocalFileRelayOptions, LocalFileOptionsValidator>();

        return services;
    }
}
