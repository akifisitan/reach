using System.Threading.Channels;
using Reach.Plugin.Abstractions;
using Reach.Relay.Abstractions;
using Reach.Shared;

namespace Reach.Remote.Agent;

internal sealed class RemoteAgent
{
    private readonly IRemoteRelay _relay;
    private readonly ILogger<RemoteAgent> _logger;
    private readonly ISingleAppInstanceLock _singleAppInstanceLock;
    private readonly ExecutorOptions _options;
    private readonly IHandlerDispatcher<RelayCommand, RelayCommandResult> _handler;
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly IReadOnlyList<IRepository> _repositories;

    private readonly Channel<RelayCommand> _channel = Channel.CreateUnbounded<RelayCommand>(
        new UnboundedChannelOptions { SingleReader = false, SingleWriter = true }
    );

    private readonly RelayCommandStore _commandStore;

    public RemoteAgent(
        IRemoteRelay relay,
        ILogger<RemoteAgent> logger,
        ISingleAppInstanceLock singleAppInstanceLock,
        IOptions<ExecutorOptions> options,
        IHandlerDispatcher<RelayCommand, RelayCommandResult> handler,
        RelayCommandStore commandIdStore,
        DbConnectionFactory dbConnectionFactory,
        IEnumerable<IRepository> repositories
    )
    {
        _relay = relay;
        _logger = logger;
        _singleAppInstanceLock = singleAppInstanceLock;
        _options = options.Value;
        _handler = handler;
        _commandStore = commandIdStore;
        _dbConnectionFactory = dbConnectionFactory;
        _repositories = repositories.ToList();
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        if (!_singleAppInstanceLock.TryAcquire("app.lock"))
        {
            _logger.ZLogInformation($"There's already a running instance, exiting...");

            return;
        }

        _dbConnectionFactory.Initialize();

        foreach (var repository in _repositories)
        {
            repository.Initialize();
        }

        _logger.ZLogInformation($"Running daemon");

        List<Task> tasks = [];

        tasks.Add(Dispatcher(cancellationToken));
        tasks.AddRange(
            Enumerable.Range(0, _options.ConsumerCount).Select(_ => Consumer(cancellationToken))
        );

        await Task.WhenAll(tasks);
    }

    private async Task Dispatcher(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await foreach (
                    var command in _relay.PollFromLocal(cancellationToken: cancellationToken)
                )
                {
                    if (_commandStore.Contains(command.Id))
                    {
                        _logger.ZLogInformation($"Already consumed {command.Id}");
                        continue;
                    }

                    await WriteToStore(command, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.ZLogError(ex, $"An error occurred");
            }
        }
    }

    private async Task Consumer(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var command = await _channel.Reader.ReadAsync(cancellationToken);

            try
            {
                // check if command result is already stored somewhere before trying again, costly operation
                var commandResult = await _handler.Handle(command, cancellationToken);

                // retry with backoff and jitter

                await _relay.PostToLocal(commandResult, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.ZLogError(ex, $"An error occurred");
            }
        }
    }

    private async Task WriteToStore(RelayCommand command, CancellationToken cancellationToken)
    {
        // make it durable if we need
        // dedupe here if needed
        _commandStore.Add(command);

        await _channel.Writer.WriteAsync(command, cancellationToken);
    }
}
