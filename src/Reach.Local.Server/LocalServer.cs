using Microsoft.Extensions.Options;

namespace Reach.Local.Server;

internal sealed class LocalServer
{
    private readonly ILogger<LocalServer> _logger;
    private readonly LocalPipeRpcServer _localRpcServer;
    private readonly ServerExecutorOptions _options;
    private readonly RemoteRelayPoller _remoteRelayPoller;
    private readonly EventQueueProcessor _eventQueueProcessor;

    public LocalServer(
        ILogger<LocalServer> logger,
        LocalPipeRpcServer localServer,
        IOptions<ServerExecutorOptions> options,
        RemoteRelayPoller remoteRelayPoller,
        EventQueueProcessor eventQueueProcessor
    )
    {
        _logger = logger;
        _localRpcServer = localServer;
        _options = options.Value;
        _remoteRelayPoller = remoteRelayPoller;
        _eventQueueProcessor = eventQueueProcessor;
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        _logger.ZLogInformation($"Running local server");

        List<Task> tasks = [];

        tasks.Add(_localRpcServer.Start(cancellationToken));
        tasks.Add(_remoteRelayPoller.Start(cancellationToken));
        tasks.AddRange(
            Enumerable
                .Range(0, _options.ConsumerCount)
                .Select(_ => _eventQueueProcessor.Start(isForRetry: false, cancellationToken))
        );
        tasks.AddRange(
            Enumerable
                .Range(0, _options.RetryConsumerCount)
                .Select(_ => _eventQueueProcessor.Start(isForRetry: true, cancellationToken))
        );

        await Task.WhenAll(tasks);
    }
}
