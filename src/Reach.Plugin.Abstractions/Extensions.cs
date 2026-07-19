using Microsoft.Extensions.DependencyInjection;

namespace Reach.Plugin.Abstractions;

public static class Extensions
{
    public static IServiceCollection AddPluginAbstractions(this IServiceCollection services)
    {
        services.AddSingleton(typeof(IHandlerDispatcher<,>), typeof(HandlerDispatcher<,>));

        return services;
    }

    /// <summary>
    /// Registers a single plugin feature (CLI command, JSON metadata, handlers) via
    /// <see cref="PluginFeatureBuilder" />.
    /// </summary>
    public static IServiceCollection AddPluginFeature(
        this IServiceCollection services,
        Action<PluginFeatureBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        configure(new PluginFeatureBuilder(services));

        return services;
    }
}
