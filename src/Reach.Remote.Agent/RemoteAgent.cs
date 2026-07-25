namespace Reach.Remote.Agent;

internal sealed class RemoteAgent
{
    private readonly ILogger<RemoteAgent> _logger;
    private readonly ExecutorOptions _options;
    private readonly LocalRelayPoller _localRelayPoller;
    private readonly CommandQueueProcessor _commandQueueProcessor;

    public RemoteAgent(
        ILogger<RemoteAgent> logger,
        IOptions<ExecutorOptions> options,
        LocalRelayPoller localRelayPoller,
        CommandQueueProcessor commandQueueProcessor
    )
    {
        _logger = logger;
        _options = options.Value;
        _localRelayPoller = localRelayPoller;
        _commandQueueProcessor = commandQueueProcessor;
    }

    public async Task Start(CancellationToken cancellationToken)
    {
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
