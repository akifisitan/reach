using Polly;
using Polly.Registry;
using Reach.DurableQueue;
using Reach.Plugin.Abstractions;
using Reach.Relay.Abstractions;

namespace Reach.Local.Server;

internal sealed class EventQueueProcessor
{
    private readonly ILogger<EventQueueProcessor> _logger;
    private readonly IDurableQueue<RelayCommandResult> _eventQueue;
    private readonly IHandlerDispatcher<RelayCommandResult, Unit> _handler;
    private readonly ResiliencePipeline _defaultRetry;

    public EventQueueProcessor(
        ILogger<EventQueueProcessor> logger,
        IDurableQueue<RelayCommandResult> eventQueue,
        IHandlerDispatcher<RelayCommandResult, Unit> handler,
        ResiliencePipelineProvider<string> resiliencePipelineProvider
    )
    {
        _logger = logger;
        _eventQueue = eventQueue;
        _handler = handler;
        _defaultRetry = resiliencePipelineProvider.GetPipeline(
            DurableQueueConstants.DefaultRetryPolicy
        );
    }

    public async Task Start(bool isForRetry, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RelayCommandResult? value;

            try
            {
                value = isForRetry
                    ? await _eventQueue.PickForRetry(cancellationToken)
                    : await _eventQueue.Pick(cancellationToken);

                if (value is null)
                {
                    await Task.Delay(Random.Shared.Next(1500, 1700), cancellationToken);

                    continue;
                }
            }
            catch (Exception ex)
            {
                _logger.ZLogError(ex, $"An error occurred while picking from the queue");

                await Task.Delay(Random.Shared.Next(2050, 2200), cancellationToken);

                continue;
            }

            var isSuccess = false;

            try
            {
                await _handler.Handle(value, cancellationToken);

                isSuccess = true;
            }
            catch (Exception ex)
            {
                _logger.ZLogError(ex, $"An error occurred while handling");
            }

            try
            {
                await _defaultRetry.ExecuteAsync(
                    async ct =>
                    {
                        if (isSuccess)
                        {
                            await _eventQueue.Complete(value, ct);
                        }
                        else
                        {
                            await _eventQueue.Fail(value, ct);
                        }
                    },
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _logger.ZLogError(ex, $"An error occurred while updating value");
            }
        }
    }
}
