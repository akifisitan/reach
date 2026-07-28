using Polly;
using Polly.Registry;
using Reach.Composition;
using Reach.Infra.DurableQueue;
using Reach.Relay;

namespace Reach.Local.Server;

internal sealed class RemoteRelayPoller
{
    private readonly ILocalRelay _relay;
    private readonly ILogger<RemoteRelayPoller> _logger;
    private readonly IDurableQueue<RelayCommandResult> _eventQueue;
    private readonly ResiliencePipeline _defaultRetry;
    private readonly ResiliencePipeline _relayPollChaos;

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
        _defaultRetry = resiliencePipelineProvider.GetPipeline(Constants.DefaultRetryPolicy);
        _relayPollChaos = resiliencePipelineProvider.GetPipeline(ChaosPolicyNames.RelayPoll);
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _relayPollChaos.ExecuteAsync(
                    async ct =>
                    {
                        await foreach (var result in _relay.PollFromRemote(cancellationToken: ct))
                        {
                            _logger.ZLogInformation($"Received from remote: {result}");

                            try
                            {
                                await _defaultRetry.ExecuteAsync(
                                    async retryCt =>
                                    {
                                        await _eventQueue.Enqueue(result, retryCt);
                                    },
                                    ct
                                );
                            }
                            catch (AlreadyQueuedException)
                            {
                                _logger.ZLogInformation($"Skipping already queued value");
                            }
                            catch (Exception ex)
                            {
                                _logger.ZLogError(
                                    ex,
                                    $"An error occurred while inserting value to queue"
                                );
                            }
                        }
                    },
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _logger.ZLogError(ex, $"An error occurred while polling from remote");

                await Task.Delay(Random.Shared.Next(2000, 2200), cancellationToken);
            }
        }
    }
}
