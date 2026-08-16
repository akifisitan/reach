using System.IO.Pipes;
using Reach.Local.Contracts;
using Reach.Module;
using StreamJsonRpc;

namespace Reach.Local.Cli;

internal sealed class LocalPipeRpcClient : IRpcClient
{
    private readonly ILogger<LocalPipeRpcClient> _logger;
    private readonly IRpcPayloadSerializer _payloadSerializer;

    public LocalPipeRpcClient(
        ILogger<LocalPipeRpcClient> logger,
        IRpcPayloadSerializer payloadSerializer
    )
    {
        _logger = logger;
        _payloadSerializer = payloadSerializer;
    }

    public async Task<TOut> Invoke<TIn, TOut>(TIn payload, CancellationToken cancellationToken)
        where TIn : RpcPayload
        where TOut : RpcResult
    {
        await using var connection = await CreateConnection(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var serializedPayload = _payloadSerializer.Serialize((RpcPayload)payload);

        var result = await connection
            .Rpc.InvokeWithCancellationAsync<string>(
                LocalRpcShared.MainMethodName,
                [serializedPayload],
                cancellationToken
            )
            .ConfigureAwait(false);

        return (TOut)_payloadSerializer.Deserialize<RpcResult>(result);
    }

    public async Task<bool> CheckConnection(CancellationToken cancellationToken)
    {
        await using var connection = await CreateConnection(
                connectionTimeoutMs: 100,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        var result = await connection
            .Rpc.InvokeWithCancellationAsync<bool>(
                LocalRpcShared.HealthCheckMethodName,
                [],
                cancellationToken
            )
            .ConfigureAwait(false);

        return result;
    }

    private async Task<RpcConnection> CreateConnection(
        int connectionTimeoutMs = 1000 * 2,
        CancellationToken cancellationToken = default
    )
    {
        _logger.ZLogInformation($"Attempting to connect to {LocalRpcShared.PipeName}");

        var pipe = new NamedPipeClientStream(
            ".",
            LocalRpcShared.PipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous
        );

        await pipe.ConnectAsync(connectionTimeoutMs, cancellationToken).ConfigureAwait(false);

        _logger.ZLogInformation($"Connected to {LocalRpcShared.PipeName}");

        var rpc = new JsonRpc(
            new LengthHeaderMessageHandler(pipe, pipe, LocalRpcShared.CreateFormatter())
        );

        rpc.StartListening();

        _logger.ZLogInformation($"Started listening to rpc");

        return new(rpc, pipe);
    }

    private sealed class RpcConnection : IAsyncDisposable
    {
        public JsonRpc Rpc { get; }
        private readonly NamedPipeClientStream _stream;

        public RpcConnection(JsonRpc jsonRpc, NamedPipeClientStream stream)
        {
            Rpc = jsonRpc;
            _stream = stream;
        }

        public async ValueTask DisposeAsync()
        {
            await _stream.DisposeAsync().ConfigureAwait(false);
            Rpc.Dispose();
        }
    }
}
