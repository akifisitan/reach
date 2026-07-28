namespace Reach.Module;

public static class Extensions
{
    /// <summary>
    /// Registers a single feature (CLI command, JSON metadata, handlers) via
    /// <see cref="ModuleFeatureBuilder" />.
    /// </summary>
    public static IServiceCollection AddFeature(
        this IServiceCollection services,
        Action<ModuleFeatureBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.TryAddSingleton(typeof(IHandlerDispatcher<,>), typeof(HandlerDispatcher<,>));

        configure(new ModuleFeatureBuilder(services));

        return services;
    }
}
