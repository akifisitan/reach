using System.IO.Pipes;
using Reach.Local.Contracts;
using StreamJsonRpc;

namespace Reach.Local.Server;

internal sealed class LocalPipeRpcServer
{
    private readonly RpcTarget _rpcTarget;
    private readonly ILogger<LocalPipeRpcServer> _logger;

    public LocalPipeRpcServer(RpcTarget rpcTarget, ILogger<LocalPipeRpcServer> logger)
    {
        _rpcTarget = rpcTarget;
        _logger = logger;
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var pipe = new NamedPipeServerStream(
                LocalRpcShared.PipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous
            );

            _logger.ZLogDebug($"Waiting for new client connection");

            try
            {
                await pipe.WaitForConnectionAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.ZLogError(ex, $"An error occurred while waiting for connection");

                await pipe.DisposeAsync();

                throw;
            }

            _logger.ZLogDebug($"Connecting to client");

            _ = Task.Run(() => HandleClient(pipe), cancellationToken);
        }
    }

    private async Task HandleClient(NamedPipeServerStream pipe)
    {
        await using var _ = pipe;

        using var rpc = new JsonRpc(
            new LengthHeaderMessageHandler(pipe, pipe, LocalRpcShared.CreateFormatter())
        );

        rpc.AddLocalRpcMethod(LocalRpcShared.MainMethodName, _rpcTarget.Invoke);
        rpc.AddLocalRpcMethod(LocalRpcShared.HealthCheckMethodName, _rpcTarget.HealthCheck);

        rpc.StartListening();

        _logger.ZLogDebug($"Connected to client");

        await rpc.Completion;

        _logger.ZLogDebug($"Disconnected from client");
    }
}
