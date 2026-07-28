namespace Reach.Module;

public interface IRpcClient
{
    Task<TOut> Invoke<TIn, TOut>(TIn payload, CancellationToken cancellationToken)
        where TIn : RpcPayload
        where TOut : RpcResult;
    Task<bool> CheckConnection(CancellationToken cancellationToken);
}

public abstract record RpcPayload;

public abstract record RpcResult;
