using System.Diagnostics.CodeAnalysis;
using Reach.Relay;

namespace Reach.Module;

/// <summary>
/// Registers everything that makes up a single feature (CLI command, JSON serialization
/// metadata, handlers) in one place. See <see cref="Extensions.AddFeature" />.
/// </summary>
public sealed class ModuleFeatureBuilder
{
    private readonly IServiceCollection _services;

    internal ModuleFeatureBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Registers the feature's CLI command. If it implements <see cref="ICliCommand" /> it is
    /// also registered as one, so the root command picks it up.
    /// </summary>
    public ModuleFeatureBuilder WithCliCommand<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TCliCommand
    >()
        where TCliCommand : class
    {
        _services.AddSingleton<TCliCommand>();

        if (typeof(ICliCommand).IsAssignableFrom(typeof(TCliCommand)))
        {
            _services.AddSingleton(serviceProvider =>
                (ICliCommand)serviceProvider.GetRequiredService<TCliCommand>()
            );
        }

        return this;
    }

    /// <summary>
    /// Registers the feature's JSON serialization metadata (polymorphic type registrations
    /// and the source-generated serializer context for the feature's types).
    /// </summary>
    public ModuleFeatureBuilder WithJson<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            TJsonTypeInfoRegistration
    >()
        where TJsonTypeInfoRegistration : class, IWireJsonRegistration
    {
        _services.AddSingleton<IWireJsonRegistration, TJsonTypeInfoRegistration>();

        return this;
    }

    /// <summary>
    /// Registers a <see cref="CliCommandHandler{TPayload, TResult}" /> (an
    /// <see cref="IHandler{TBasePayload, TBaseResult}" /> of <see cref="CliCommandPayload" />/
    /// <see cref="CliCommandResult" />).
    /// </summary>
    public ModuleFeatureBuilder WithCliCommandHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler
    >()
        where THandler : class, IHandler<CliCommandPayload, CliCommandResult> =>
        WithHandler<THandler, CliCommandPayload, CliCommandResult>();

    /// <summary>
    /// Registers an <see cref="RpcHandler{TPayload, TResult}" /> (an
    /// <see cref="IHandler{TBasePayload, TBaseResult}" /> of <see cref="RpcPayload" />/
    /// <see cref="RpcResult" />).
    /// </summary>
    public ModuleFeatureBuilder WithRpcHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler
    >()
        where THandler : class, IHandler<RpcPayload, RpcResult> =>
        WithHandler<THandler, RpcPayload, RpcResult>();

    /// <summary>
    /// Registers a <see cref="RelayCommandHandler{TPayload, TResult}" /> (an
    /// <see cref="IHandler{TBasePayload, TBaseResult}" /> of <see cref="RelayCommand" />/
    /// <see cref="RelayCommandResult" />).
    /// </summary>
    public ModuleFeatureBuilder WithRelayCommandHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler
    >()
        where THandler : class, IHandler<RelayCommand, RelayCommandResult> =>
        WithHandler<THandler, RelayCommand, RelayCommandResult>();

    /// <summary>
    /// Registers a <see cref="RelayCommandResultHandler{TPayload}" /> (an
    /// <see cref="IHandler{TBasePayload, TBaseResult}" /> of <see cref="RelayCommandResult" />/
    /// <see cref="Unit" />).
    /// </summary>
    public ModuleFeatureBuilder WithRelayCommandResultHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler
    >()
        where THandler : class, IHandler<RelayCommandResult, Unit> =>
        WithHandler<THandler, RelayCommandResult, Unit>();

    public ModuleFeatureBuilder WithRemoteCommandRpcHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler
    >()
        where THandler : class, IHandler<RpcPayload, RpcResult>, IRemoteCommandRpcHandler
    {
        _services.AddSingleton<THandler>();
        _services.AddSingleton<IHandler<RpcPayload, RpcResult>>(serviceProvider =>
            serviceProvider.GetRequiredService<THandler>()
        );
        _services.AddSingleton<IRemoteCommandRpcHandler>(serviceProvider =>
            serviceProvider.GetRequiredService<THandler>()
        );
        _services.AddSingleton(HandlerDescriptor.Create<THandler, RpcPayload, RpcResult>());

        return this;
    }

    /// <summary>
    /// Registers a handler for the given base payload/result pair
    /// (e.g. <see cref="RpcPayload" />/<see cref="RpcResult" />).
    /// </summary>
    public ModuleFeatureBuilder WithHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler,
        TBasePayload,
        TBaseResult
    >()
        where THandler : class, IHandler<TBasePayload, TBaseResult>
    {
        _services.AddSingleton<IHandler<TBasePayload, TBaseResult>, THandler>();
        _services.AddSingleton(HandlerDescriptor.Create<THandler, TBasePayload, TBaseResult>());

        return this;
    }
}
