using Reach.Composition;

namespace Reach.Local.Shared;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLocalShared(
        this IServiceCollection services,
        IConfiguration configuration,
        string basePath
    )
    {
        services.AddComposition(configuration, basePath);

        return services;
    }
}
