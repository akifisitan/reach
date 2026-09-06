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
        services.AddSingleton(new LocalFileConfigurationStore(basePath));
        services.AddSingleton<LocalFileRelay>();
        services.AddSingleton<ILocalRelay>(sp => sp.GetRequiredService<LocalFileRelay>());
        services.AddSingleton<IRemoteRelay>(sp => sp.GetRequiredService<LocalFileRelay>());
        services.AddSingleton<IRelayTextEncoding>(
            new BrotliCompressedRelayTextEncoding(new Base64RelayTextEncoding())
        );
        //services.AddSingleton<IRelayTextEncoding>(new Utf8RelayTextEncoding());

        services.AddSingleton<IRelaySetup, LocalFileRelaySetup>();
        services.AddSingleton<IStartupCheck, LocalFileStartupCheck>();

        return services;
    }
}
