using System.CommandLine;
using System.Text.Json.Serialization.Metadata;
using Reach.Relay.Abstractions;

namespace Reach.Plugin.Abstractions;

// having separate interfaces is good since not everywhere requires everything the plugin has
// problem is how do we ensure the interfaces are registered for a specific type?
// could do this at build time or at runtime
// if we do it at build time it would be very coupled, but we kinda do want that?
// could have a validator that checks if everything resolves from di?

internal abstract class PluginHandler<TRelayCommand, TRelayCommandResult, TRpcPayload, TRpcResult>
    where TRelayCommand : RelayCommand<TRelayCommandResult>
    where TRelayCommandResult : RelayCommandResult
    where TRpcPayload : RpcPayload
    where TRpcResult : RpcResult
{
    public void Register(JsonTypeInfo typeInfo)
    {
        ConfigurePolymorphism(typeInfo);

        Register();
    }

    protected abstract void Register();

    // map rpc payload to relay command before writing to remote
    public abstract TRelayCommand MapToRelayCommand(TRpcPayload payload);

    // map write to remote result to RpcResult
    public abstract RpcResult Map(TRpcPayload payload);

    // Local | Cli | Handles what will happen when the cli is invoked by the user
    public abstract Task Handle(ParseResult parseResult, CancellationToken cancellationToken);

    // Local | Server | Handles communication between cli and server
    public abstract Task<TRpcResult> Handle(
        TRpcPayload payload,
        CancellationToken cancellationToken
    );

    // Remote | Agent | Handles what will happen when the command is received on the remote
    public abstract Task<TRelayCommandResult> Handle(
        TRelayCommand command,
        CancellationToken cancellationToken
    );

    // Local | Server | Handles what will happen when the command result is received from remote
    public abstract Task Handle(TRelayCommandResult result, CancellationToken cancellationToken);

    private static void ConfigurePolymorphism(JsonTypeInfo typeInfo)
    {
        typeInfo.RegisterDerivedWithTypeNameAsDiscriminator<RelayCommand, TRelayCommand>();
        typeInfo.RegisterDerivedWithTypeNameAsDiscriminator<
            RelayCommand<TRelayCommandResult>,
            TRelayCommand
        >();
        typeInfo.RegisterDerivedWithTypeNameAsDiscriminator<
            RelayCommandResult,
            TRelayCommandResult
        >();
        typeInfo.RegisterDerivedWithTypeNameAsDiscriminator<RpcPayload, TRpcPayload>();
        typeInfo.RegisterDerivedWithTypeNameAsDiscriminator<RpcResult, TRpcResult>();
    }
}
