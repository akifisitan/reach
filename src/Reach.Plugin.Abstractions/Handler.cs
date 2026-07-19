using System.Collections.Frozen;
using Reach.Relay.Abstractions;

namespace Reach.Plugin.Abstractions;

public interface IHandler<TBasePayload, TBaseResult>
{
    Type PayloadType { get; }
    Task<TBaseResult> Handle(TBasePayload payload, CancellationToken cancellationToken);
}

public interface IHandlerDispatcher<TBasePayload, TBaseResult>
{
    Task<TBaseResult> Handle(TBasePayload payload, CancellationToken cancellationToken);
}

public sealed class HandlerDispatcher<TBasePayload, TBaseResult>
    : IHandlerDispatcher<TBasePayload, TBaseResult>
{
    private readonly FrozenDictionary<Type, IHandler<TBasePayload, TBaseResult>> _handlers;

    public HandlerDispatcher(IEnumerable<IHandler<TBasePayload, TBaseResult>> handlers)
    {
        _handlers = handlers.ToFrozenDictionary(x => x.PayloadType);
    }

    public Task<TBaseResult> Handle(TBasePayload payload, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (!_handlers.TryGetValue(payload.GetType(), out var handler))
        {
            throw new NotSupportedException(
                $"No {nameof(IHandler<,>)} registered for payload type '{payload.GetType().FullName}'"
            );
        }

        return handler.Handle(payload, cancellationToken);
    }
}

public abstract class Handler<TPayload, TResult, TBasePayload, TBaseResult>
    : IHandler<TBasePayload, TBaseResult>
    where TPayload : TBasePayload
    where TBasePayload : notnull
    where TResult : TBaseResult
{
    public Type PayloadType => typeof(TPayload);

    public async Task<TBaseResult> Handle(
        TBasePayload payload,
        CancellationToken cancellationToken
    ) => await Handle((TPayload)payload, cancellationToken);

    protected abstract Task<TResult> Handle(TPayload payload, CancellationToken cancellationToken);
}

public abstract class RelayCommandHandler<TPayload, TResult>
    : Handler<TPayload, TResult, RelayCommand, RelayCommandResult>
    where TPayload : RelayCommand
    where TResult : RelayCommandResult;

public abstract class RelayCommandResultHandler<TPayload>
    : Handler<TPayload, Unit, RelayCommandResult, Unit>
    where TPayload : RelayCommandResult;

public abstract class CliCommandHandler<TPayload, TResult>
    : Handler<TPayload, TResult, CliCommandPayload, CliCommandResult>
    where TPayload : CliCommandPayload
    where TResult : CliCommandResult;

public abstract class RpcHandler<TPayload, TResult>
    : Handler<TPayload, TResult, RpcPayload, RpcResult>
    where TPayload : RpcPayload
    where TResult : RpcResult;
