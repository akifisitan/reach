using System.IO.Pipes;
using Nerdbank.JsonRpc;
using Reach.Local.Contracts;
using Reach.Module;

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
        var connection = await CreateConnection(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        await using var connectionScope = connection.ConfigureAwait(false);

        var serializedPayload = _payloadSerializer.Serialize((RpcPayload)payload);

        var result = await connection
            .Client.Invoke(serializedPayload, cancellationToken)
            .ConfigureAwait(false);

        return (TOut)_payloadSerializer.Deserialize<RpcResult>(result);
    }

    public async Task<bool> CheckConnection(CancellationToken cancellationToken)
    {
        var connection = await CreateConnection(
                connectionTimeoutMs: 100,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
        await using var connectionScope = connection.ConfigureAwait(false);

        var result = await connection.Client.HealthCheck(cancellationToken).ConfigureAwait(false);

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

        var rpc = LocalRpcShared.CreateConnection(pipe);

        rpc.Start();

        _logger.ZLogInformation($"Started listening to rpc");

        return new(rpc, pipe);
    }

    private sealed class RpcConnection : IAsyncDisposable
    {
        public ILocalRpc Client { get; }
        private readonly JsonRpc _rpc;
        private readonly NamedPipeClientStream _stream;

        public RpcConnection(JsonRpc jsonRpc, NamedPipeClientStream stream)
        {
            _rpc = jsonRpc;
            Client = jsonRpc.Attach<ILocalRpc>();
            _stream = stream;
        }

        public async ValueTask DisposeAsync()
        {
            _rpc.Dispose();
            await _stream.DisposeAsync().ConfigureAwait(false);
        }
    }
}
