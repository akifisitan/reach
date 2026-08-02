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
    private readonly IDurableQueue<RelayCommandResult> _eventQueue;
    private readonly ResiliencePipeline _retryPipeline;
    private readonly ResiliencePipeline _relayChaosPipeline;

    public RemoteRelayPoller(
        ILocalRelay relay,
        ILogger<RemoteRelayPoller> logger,
        IDurableQueue<RelayCommandResult> eventQueue,
        ResiliencePipelineProvider<string> resiliencePipelineProvider
    )
    {
        _relay = relay;
        _logger = logger;
        _eventQueue = eventQueue;
        _retryPipeline = resiliencePipelineProvider.GetPipeline(Constants.DefaultRetryPolicy);
        _relayChaosPipeline = resiliencePipelineProvider.GetPipeline(ChaosPolicyNames.RelayPoll);
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await using var enumerator = _relay
                    .PollFromRemote(cancellationToken: cancellationToken)
                    .GetAsyncEnumerator(cancellationToken);

                while (
                    await _relayChaosPipeline.ExecuteAsync(
                        _ => enumerator.MoveNextAsync(),
                        cancellationToken
                    )
                )
                {
                    var result = enumerator.Current;

                    _logger.ZLogInformation($"Received from remote: {result}");

                    try
                    {
                        await _retryPipeline.ExecuteAsync(
                            async ct =>
                            {
                                await _eventQueue.Enqueue(result, ct);
                            },
                            cancellationToken
                        );
                    }
                    catch (AlreadyQueuedException)
                    {
                        _logger.ZLogInformation($"Skipping already queued value");
                    }
                    catch (Exception ex)
                    {
                        _logger.ZLogError(ex, $"An error occurred while inserting value to queue");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.ZLogError(ex, $"An error occurred while polling from remote");

                await Task.Delay(Random.Shared.Next(2000, 2200), cancellationToken);
            }
        }
    }
}
