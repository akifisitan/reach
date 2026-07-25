namespace Reach.Relay.LocalFile;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLocalFileRelay(this IServiceCollection services)
    {
        services.AddSingleton<ILocalRelay, LocalFileRelay>();
        services.AddSingleton<IRemoteRelay, LocalFileRelay>();

        return services;
    }
}
