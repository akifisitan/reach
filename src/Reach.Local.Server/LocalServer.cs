using Microsoft.Extensions.Options;
using Reach.Persistence;
using Reach.Shared;

namespace Reach.Local.Server;

internal sealed class LocalServer
{
    private readonly ISingleAppInstanceLock _singleAppInstanceLock;
    private readonly ILogger<LocalServer> _logger;
    private readonly LocalPipeRpcServer _localRpcServer;
    private readonly ServerExecutorOptions _options;
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly IReadOnlyList<IDbInitializable> _repositories;
    private readonly RemoteRelayPoller _remoteRelayPoller;
    private readonly EventQueueProcessor _eventQueueProcessor;

    public LocalServer(
        ILogger<LocalServer> logger,
        ISingleAppInstanceLock singleAppInstanceLock,
        LocalPipeRpcServer localServer,
        IOptions<ServerExecutorOptions> options,
        DbConnectionFactory dbConnectionFactory,
        IEnumerable<IDbInitializable> repositories,
        RemoteRelayPoller remoteRelayPoller,
        EventQueueProcessor eventQueueProcessor
    )
    {
        _logger = logger;
        _singleAppInstanceLock = singleAppInstanceLock;
        _localRpcServer = localServer;
        _options = options.Value;
        _dbConnectionFactory = dbConnectionFactory;
        _repositories = repositories.ToList();
        _remoteRelayPoller = remoteRelayPoller;
        _eventQueueProcessor = eventQueueProcessor;
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        if (!_singleAppInstanceLock.TryAcquire(AppPaths.LockFilePath))
        {
            return;
        }

        _dbConnectionFactory.Initialize();

        foreach (var repository in _repositories)
        {
            repository.EnsureCreated();
        }

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
