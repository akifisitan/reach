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
                    .PollFromRemote(cancellationToken: cancellationToken)
                    .GetAsyncEnumerator(cancellationToken);

                while (
                    await _chaosPipeline.ExecuteAsync(
                        _ => enumerator.MoveNextAsync(),
                        cancellationToken
                    )
                )
                {
                    var result = enumerator.Current;

                    _logger.ZLogDebug($"Received result from remote: {result}");

                    await EnqueueResult(result, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(ex, $"An error occurred while polling from remote");

                await Task.Delay(Random.Shared.Next(2000, 2200), cancellationToken);
            }
        }
    }

    private async Task EnqueueResult(RelayCommandResult result, CancellationToken cancellationToken)
    {
        try
        {
            await _retryPipeline.ExecuteAsync(
                async ct =>
                {
                    await _queue.Enqueue(result, ct);
                },
                cancellationToken
            );

            _logger.ZLogInformation($"Queued result {result.CommandId} from remote relay");
        }
        catch (AlreadyQueuedException)
        {
            _logger.ZLogDebug($"Skipping already queued result {result.CommandId}");
        }
        catch (Exception ex)
        {
            _logger.ZLogError(
                ex,
                $"An error occurred while inserting result {result.CommandId} to queue"
            );
        }
    }
}
