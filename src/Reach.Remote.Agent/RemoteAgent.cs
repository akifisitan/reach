using Reach.Persistence;
using Reach.Shared;

namespace Reach.Remote.Agent;

internal sealed class RemoteAgent
{
    private readonly ILogger<RemoteAgent> _logger;
    private readonly ISingleAppInstanceLock _singleAppInstanceLock;
    private readonly ExecutorOptions _options;
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly IReadOnlyList<IDbInitializable> _repositories;
    private readonly LocalRelayPoller _localRelayPoller;
    private readonly CommandQueueProcessor _commandQueueProcessor;

    public RemoteAgent(
        ILogger<RemoteAgent> logger,
        ISingleAppInstanceLock singleAppInstanceLock,
        IOptions<ExecutorOptions> options,
        DbConnectionFactory dbConnectionFactory,
        IEnumerable<IDbInitializable> repositories,
        LocalRelayPoller localRelayPoller,
        CommandQueueProcessor commandQueueProcessor
    )
    {
        _logger = logger;
        _singleAppInstanceLock = singleAppInstanceLock;
        _options = options.Value;
        _dbConnectionFactory = dbConnectionFactory;
        _repositories = repositories.ToList();
        _localRelayPoller = localRelayPoller;
        _commandQueueProcessor = commandQueueProcessor;
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        if (!_singleAppInstanceLock.TryAcquire(AppPaths.LockFilePath))
        {
            _logger.ZLogInformation($"There's already a running instance, exiting...");

            return;
        }

        _dbConnectionFactory.Initialize();

        foreach (var repository in _repositories)
        {
            repository.EnsureCreated();
        }

        _logger.ZLogInformation($"Running daemon");

        List<Task> tasks = [];

        tasks.Add(_localRelayPoller.Start(cancellationToken));
        tasks.AddRange(
            Enumerable
                .Range(0, _options.ConsumerCount)
                .Select(_ => _commandQueueProcessor.Start(isForRetry: false, cancellationToken))
        );
        tasks.AddRange(
            Enumerable
                .Range(0, _options.RetryConsumerCount)
                .Select(_ => _commandQueueProcessor.Start(isForRetry: true, cancellationToken))
        );

        await Task.WhenAll(tasks);
    }
}
