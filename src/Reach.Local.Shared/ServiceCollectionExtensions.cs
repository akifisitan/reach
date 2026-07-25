using Reach.Composition;

namespace Reach.Local.Shared;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLocalShared(this IServiceCollection services)
    {
        services.AddComposition();

        return services;
    }
}
