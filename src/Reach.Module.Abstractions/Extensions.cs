using System.Diagnostics.CodeAnalysis;

namespace Reach.Module;

public static class Extensions
{
    public static IServiceCollection AddRootCliCommand<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TCommand
    >(this IServiceCollection services)
        where TCommand : class, IRootCliCommand
    {
        ArgumentNullException.ThrowIfNull(services);

        if (
            services.Any(descriptor =>
                descriptor.ServiceType == typeof(IRootCliCommand)
                && descriptor.ImplementationType == typeof(TCommand)
            )
        )
        {
            throw new InvalidOperationException(
                $"Root CLI command '{typeof(TCommand).FullName}' is already registered."
            );
        }

        services.AddSingleton<IRootCliCommand, TCommand>();

        return services;
    }

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
