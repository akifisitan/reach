using Polly;
using Polly.Registry;
using Reach.DurableQueue;
using Reach.Relay.Abstractions;

namespace Reach.Remote.Agent;

internal sealed class LocalRelayPoller
{
    private readonly IRemoteRelay _relay;
    private readonly ILogger<LocalRelayPoller> _logger;
    private readonly IDurableQueue<RelayCommand> _commandQueue;
    private readonly ResiliencePipeline _defaultRetry;

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
        _defaultRetry = resiliencePipelineProvider.GetPipeline(
            DurableQueueConstants.DefaultRetryPolicy
        );
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await foreach (
                    var command in _relay.PollFromLocal(cancellationToken: cancellationToken)
                )
                {
                    _logger.ZLogInformation($"Received from local: {command}");

                    try
                    {
                        await _defaultRetry.ExecuteAsync(
                            async ct =>
                            {
                                await _commandQueue.Enqueue(command, ct);
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
                _logger.ZLogError(ex, $"An error occurred while polling from local");

                await Task.Delay(Random.Shared.Next(2000, 2200), cancellationToken);
            }
        }
    }
}
