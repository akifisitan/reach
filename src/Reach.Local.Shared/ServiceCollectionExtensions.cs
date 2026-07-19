using Reach.Shared;

namespace Reach.Local.Shared;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLocalShared(this IServiceCollection services)
    {
        services.AddShared();

        return services;
    }
}
