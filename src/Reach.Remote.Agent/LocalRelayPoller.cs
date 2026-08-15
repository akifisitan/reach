using Polly;
using Polly.Registry;
using Reach.Hosting;
using Reach.Infra.DurableQueue;
using Reach.Relay;

namespace Reach.Remote.Agent;

internal sealed class LocalRelayPoller
{
    private readonly IRemoteRelay _relay;
    private readonly ILogger<LocalRelayPoller> _logger;
    private readonly IDurableQueue<RelayCommand> _commandQueue;
    private readonly ResiliencePipeline _retryPipeline;
    private readonly ResiliencePipeline _chaosPipeline;

    public LocalRelayPoller(
        IRemoteRelay relay,
        ILogger<LocalRelayPoller> logger,
        IDurableQueue<RelayCommand> commandQueue,
        ResiliencePipelineProvider<string> resiliencePipelineProvider
    )
    {
        _relay = relay;
        _logger = logger;
        _commandQueue = commandQueue;
        _retryPipeline = resiliencePipelineProvider.GetPipeline(Constants.DefaultRetryPolicy);
        _chaosPipeline = resiliencePipelineProvider.GetPipeline(ChaosPolicyNames.RelayPoll);
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var enumerator = _relay
                    .PollFromLocal(cancellationToken: cancellationToken)
                    .GetAsyncEnumerator(cancellationToken);

                while (
                    await _chaosPipeline.ExecuteAsync(
                        _ => enumerator.MoveNextAsync(),
                        cancellationToken
                    )
                )
                {
                    var command = enumerator.Current;

                    _logger.ZLogDebug($"Received command from local: {command}");

                    await EnqueueCommand(command, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(ex, $"An error occurred while polling from local");

                await Task.Delay(Random.Shared.Next(2000, 2200), cancellationToken);
            }
        }
    }

    // TODO: This flow auto-acks even if the command cannot be enqueued. Can we improve that?
    private async Task EnqueueCommand(RelayCommand command, CancellationToken cancellationToken)
    {
        try
        {
            await _retryPipeline.ExecuteAsync(
                async ct =>
                {
                    await _commandQueue.Enqueue(command, ct);
                },
                cancellationToken
            );

            _logger.ZLogInformation($"Queued command {command.Id} from local relay");
        }
        catch (AlreadyQueuedException)
        {
            _logger.ZLogDebug($"Skipping already queued command {command.Id}");
        }
        catch (Exception ex)
        {
            // value lost
            _logger.ZLogError(
                ex,
                $"An error occurred while inserting command {command.Id} to queue"
            );
        }
    }
}
