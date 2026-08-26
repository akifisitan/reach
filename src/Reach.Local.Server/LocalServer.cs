using Microsoft.Extensions.Options;

namespace Reach.Local.Server;

internal sealed class LocalServer
{
    private readonly ILogger<LocalServer> _logger;
    private readonly LocalPipeRpcServer _localRpcServer;
    private readonly ServerExecutorOptions _options;
    private readonly RemoteRelayPoller _remoteRelayPoller;
    private readonly RelayCommandResultQueueProcessor _queueProcessor;

    public LocalServer(
        ILogger<LocalServer> logger,
        LocalPipeRpcServer localServer,
        IOptions<ServerExecutorOptions> options,
        RemoteRelayPoller remoteRelayPoller,
        RelayCommandResultQueueProcessor queueProcessor
    )
    {
        _logger = logger;
        _localRpcServer = localServer;
        _options = options.Value;
        _remoteRelayPoller = remoteRelayPoller;
        _queueProcessor = queueProcessor;
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        _logger.ZLogInformation($"Running local server");

        List<Task> tasks = [];

        tasks.Add(_localRpcServer.Start(cancellationToken));
        tasks.Add(_remoteRelayPoller.Start(cancellationToken));
        tasks.AddRange(
            Enumerable
                .Range(0, _options.QueueProcessorCount)
                .Select(_ => _queueProcessor.Start(isForRetry: false, cancellationToken))
        );
        tasks.AddRange(
            Enumerable
                .Range(0, _options.RetryQueueProcessorCount)
                .Select(_ => _queueProcessor.Start(isForRetry: true, cancellationToken))
        );

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }
}
