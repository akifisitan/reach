using Polly;
using Polly.Registry;
using Reach.Hosting;
using Reach.Infra.DurableQueue;
using Reach.Relay;

namespace Reach.Local.Server;

internal sealed class RemoteRelayPoller
{
    private readonly ILocalRelay _relay;
    private readonly ILogger<RemoteRelayPoller> _logger;
    private readonly IDurableQueue<RelayCommandResult> _queue;
    private readonly ResiliencePipeline _retryPipeline;
    private readonly ResiliencePipeline _chaosPipeline;

    public RemoteRelayPoller(
        ILocalRelay relay,
        ILogger<RemoteRelayPoller> logger,
        IDurableQueue<RelayCommandResult> queue,
        ResiliencePipelineProvider<string> resiliencePipelineProvider
    )
    {
        _relay = relay;
        _logger = logger;
        _queue = queue;
        _retryPipeline = resiliencePipelineProvider.GetPipeline(RetryPolicyNames.Default);
        _chaosPipeline = resiliencePipelineProvider.GetPipeline(ChaosPolicyNames.RelayPoll);
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var enumerator = _relay
                    .PollFromRemote(cancellationToken: cancellationToken)
                    .GetAsyncEnumerator(cancellationToken);
                await using var enumeratorScope = enumerator.ConfigureAwait(false);

                while (
                    await _chaosPipeline
                        .ExecuteAsync(_ => enumerator.MoveNextAsync(), cancellationToken)
                        .ConfigureAwait(false)
                )
                {
                    var delivery = enumerator.Current;
                    var result = delivery.Value;

                    _logger.ZLogDebug($"Received result from remote: {result}");

                    if (!await EnqueueResult(result, cancellationToken).ConfigureAwait(false))
                    {
                        break;
                    }

                    if (!await Commit(delivery, cancellationToken).ConfigureAwait(false))
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
                _logger.ZLogWarning(ex, $"An error occurred while polling from remote");
            }

            await Task.Delay(Random.Shared.Next(2000, 2200), cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<bool> EnqueueResult(
        RelayCommandResult result,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await _retryPipeline
                .ExecuteAsync(
                    async ct =>
                    {
                        await _queue.Enqueue(result, ct).ConfigureAwait(false);
                    },
                    cancellationToken
                )
                .ConfigureAwait(false);

            _logger.ZLogInformation($"Queued result {result.CommandId} from remote relay");

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (AlreadyQueuedException)
        {
            _logger.ZLogDebug($"Skipping already queued result {result.CommandId}");

            return true;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"An error occurred while queueing result {result.CommandId}");

            return false;
        }
    }

    private async Task<bool> Commit(
        RelayDelivery<RelayCommandResult> delivery,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await delivery.Commit(cancellationToken).ConfigureAwait(false);

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
                $"An error occurred while committing delivery {delivery.Value.CommandId}"
            );

            return false;
        }
    }
}
