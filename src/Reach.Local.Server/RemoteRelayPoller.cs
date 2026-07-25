using Polly;
using Polly.Registry;
using Reach.DurableQueue;
using Reach.Relay.Abstractions;

namespace Reach.Local.Server;

internal sealed class RemoteRelayPoller
{
    private readonly ILocalRelay _relay;
    private readonly ILogger<RemoteRelayPoller> _logger;
    private readonly IDurableQueue<RelayCommandResult> _eventQueue;
    private readonly ResiliencePipeline _defaultRetry;

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
        _defaultRetry = resiliencePipelineProvider.GetPipeline(AppConstants.DefaultRetryPolicy);
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await foreach (
                    var result in _relay.PollFromRemote(cancellationToken: cancellationToken)
                )
                {
                    _logger.ZLogInformation($"Received from remote: {result}");

                    try
                    {
                        await _defaultRetry.ExecuteAsync(
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
