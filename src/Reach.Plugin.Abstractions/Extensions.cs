using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Reach.Relay.Abstractions;

namespace Reach.Plugin.Abstractions;

public static class Extensions
{
    public static IServiceCollection AddPluginAbstractions(this IServiceCollection services)
    {
        services.AddSingleton(typeof(IHandlerDispatcher<,>), typeof(HandlerDispatcher<,>));

        return services;
    }

    public static IServiceCollection AddHandlers<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            TCliCommandHandler,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            TRpcPayloadHandler
    >(this IServiceCollection services)
        where TCliCommandHandler : class, IHandler<CliCommandPayload, CliCommandResult>
        where TRpcPayloadHandler : class, IHandler<RpcPayload, RpcResult>
    {
        services.AddSingleton<IHandler<CliCommandPayload, CliCommandResult>, TCliCommandHandler>();
        services.AddSingleton<IHandler<RpcPayload, RpcResult>, TRpcPayloadHandler>();

        return services;
    }

    public static IServiceCollection AddHandlers<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            TCliCommandHandler,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            TRpcPayloadHandler,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            TRelayCommandHandler,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            TRelayCommandResultHandler
    >(this IServiceCollection services)
        where TCliCommandHandler : class, IHandler<CliCommandPayload, CliCommandResult>
        where TRpcPayloadHandler : class, IHandler<RpcPayload, RpcResult>
        where TRelayCommandHandler : class, IHandler<RelayCommand, RelayCommandResult>
        where TRelayCommandResultHandler : class, IHandler<RelayCommandResult, Unit>
    {
        services.AddSingleton<IHandler<CliCommandPayload, CliCommandResult>, TCliCommandHandler>();
        services.AddSingleton<IHandler<RpcPayload, RpcResult>, TRpcPayloadHandler>();
        services.AddSingleton<IHandler<RelayCommand, RelayCommandResult>, TRelayCommandHandler>();
        services.AddSingleton<IHandler<RelayCommandResult, Unit>, TRelayCommandResultHandler>();

        return services;
    }
}
