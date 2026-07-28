using Reach.Hosting;

namespace Reach.Local.Shared;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLocalShared(
        this IServiceCollection services,
        IConfiguration configuration,
        string basePath
    )
    {
        services.AddHosting(configuration, basePath);

        return services;
    }
}
