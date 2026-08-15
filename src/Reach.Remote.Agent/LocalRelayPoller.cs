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
                    var delivery = enumerator.Current;
                    var command = delivery.Value;

                    _logger.ZLogDebug($"Received command from local: {command}");

                    if (!await EnqueueCommand(command, cancellationToken))
                    {
                        break;
                    }

                    if (!await Commit(delivery, cancellationToken))
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(ex, $"An error occurred while polling from local");
            }

            await Task.Delay(Random.Shared.Next(2000, 2200), cancellationToken);
        }
    }

    private async Task<bool> EnqueueCommand(
        RelayCommand command,
        CancellationToken cancellationToken
    )
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

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (AlreadyQueuedException)
        {
            _logger.ZLogDebug($"Skipping already queued command {command.Id}");

            return true;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"An error occurred while queueing command {command.Id}");

            return false;
        }
    }

    private async Task<bool> Commit(
        RelayDelivery<RelayCommand> delivery,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await delivery.Commit(cancellationToken);

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(
                ex,
                $"An error occurred while committing delivery {delivery.Value.Id}"
            );

            return false;
        }
    }
}
