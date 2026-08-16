using Reach.Relay;

namespace Reach.Module;

public abstract class RpcHandler<TPayload, TResult>
    : Handler<TPayload, TResult, RpcPayload, RpcResult>
    where TPayload : RpcPayload
    where TResult : RpcResult;

public abstract class RemoteCommandRpcHandler<TPayload, TCommand>
    : Handler<TPayload, RemoteCommandRequest, RpcPayload, RpcResult>,
        IRemoteCommandRpcHandler
    where TPayload : RpcPayload
    where TCommand : RelayCommand
{
    Type IRemoteCommandRpcHandler.CommandType => typeof(TCommand);

    protected sealed override async Task<RemoteCommandRequest> Handle(
        TPayload payload,
        CancellationToken cancellationToken
    ) => new(await PrepareCommand(payload, cancellationToken));

    Task IRemoteCommandRpcHandler.HandleSubmissionFailure(
        RelayCommand command,
        Exception exception,
        CancellationToken cancellationToken
    ) => HandleSubmissionFailure((TCommand)command, exception, cancellationToken);

    protected abstract Task<TCommand> PrepareCommand(
        TPayload payload,
        CancellationToken cancellationToken
    );

    protected abstract Task HandleSubmissionFailure(
        TCommand command,
        Exception exception,
        CancellationToken cancellationToken
    );
}

public interface IRpcClient
{
    Task<TOut> Invoke<TIn, TOut>(TIn payload, CancellationToken cancellationToken)
        where TIn : RpcPayload
        where TOut : RpcResult;
    Task<bool> CheckConnection(CancellationToken cancellationToken);
}

public abstract record RpcPayload;

public abstract record RpcResult;

public sealed record RemoteCommandRequest : RpcResult
{
    internal RemoteCommandRequest(RelayCommand command)
    {
        Command = command;
    }

    public RelayCommand Command { get; }
    public string CorrelationId => Command.Id;
}

public abstract record RemoteCommandSubmissionResult : RpcResult
{
    public sealed record Accepted(string CorrelationId) : RemoteCommandSubmissionResult;

    public sealed record Failed(string CorrelationId, RemoteCommandSubmissionError Error)
        : RemoteCommandSubmissionResult;
}

public sealed record RemoteCommandSubmissionError(string Code, string Message);

public static class RemoteCommandSubmissionErrorCodes
{
    public const string SubmissionFailed = "submission_failed";
}

public interface IRemoteCommandRpcHandler
{
    Type CommandType { get; }

    Task HandleSubmissionFailure(
        RelayCommand command,
        Exception exception,
        CancellationToken cancellationToken
    );
}
