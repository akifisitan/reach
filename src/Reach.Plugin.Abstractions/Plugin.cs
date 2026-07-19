using System.Text.Json.Serialization.Metadata;
using Reach.Relay.Abstractions;

namespace Reach.Plugin.Abstractions;

// having separate interfaces is good since not everywhere requires everything the plugin has
// problem is how do we ensure the interfaces are registered for a specific type?
// could do this at build time or at runtime
// if we do it at build time it would be very coupled, but we kinda do want that?
// could have a validator that checks if everything resolves from di?
// how do we improve dx so go to reference finds the correct handler? can we?

internal abstract class Plugin<
    TCliCommandPayload,
    TRpcPayload,
    TRelayCommand,
    TCliCommandResult,
    TRpcResult,
    TRelayCommandResult
>
    where TCliCommandPayload : CliCommandPayload
    where TRpcPayload : RpcPayload
    where TRelayCommand : RelayCommand
    where TCliCommandResult : CliCommandResult
    where TRpcResult : RpcResult
    where TRelayCommandResult : RelayCommandResult
{
    public void Register(JsonTypeInfo typeInfo)
    {
        ConfigurePolymorphism(typeInfo);

        Register();
    }

    protected abstract void Register();
    protected abstract void ConfigurePolymorphism(JsonTypeInfo typeInfo);

    // Local | Cli | Handles user cli invocation. Calls Task<TRpcResult> Handle(TRpcPayload payload) via local proxy
    public abstract Task<TCliCommandResult> Handle(
        CliCommandPayload command,
        CancellationToken cancellationToken
    );

    // Local | Server | Handles RpcPayload sent by the local cli. May or may not send a RelayCommand to remote
    public abstract Task<TRpcResult> Handle(
        TRpcPayload payload,
        CancellationToken cancellationToken
    );

    // Remote | Agent | Handles RelayCommand sent from the local server. Sends a RelayCommandResult to local
    public abstract Task<TRelayCommandResult> Handle(
        TRelayCommand command,
        CancellationToken cancellationToken
    );

    // Local | Server | Handles RelayCommandResult sent from the remote agent
    public abstract Task Handle(TRelayCommandResult result, CancellationToken cancellationToken);
}
