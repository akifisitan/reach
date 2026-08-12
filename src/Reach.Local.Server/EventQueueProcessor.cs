using Polly;
using Polly.Registry;
using Reach.Hosting;
using Reach.Infra.DurableQueue;
using Reach.Module;
using Reach.Relay;

namespace Reach.Local.Server;

internal sealed class EventQueueProcessor
{
    private readonly ILogger<EventQueueProcessor> _logger;
    private readonly IDurableQueue<RelayCommandResult> _eventQueue;
    private readonly IHandlerDispatcher<RelayCommandResult, Unit> _handler;
    private readonly ResiliencePipeline _defaultRetry;
    private readonly ResiliencePipeline _relayResultChaos;

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
        _defaultRetry = resiliencePipelineProvider.GetPipeline(Constants.DefaultRetryPolicy);
        _relayResultChaos = resiliencePipelineProvider.GetPipeline(ChaosPolicyNames.RelayResult);
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
                await _relayResultChaos.ExecuteAsync(
                    async ct => await _handler.Handle(value, ct),
                    cancellationToken
                );

                isSuccess = true;

                _logger.ZLogDebug($"Handled result {value.CommandId}");
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(
                    ex,
                    $"An error occurred while handling result {value.CommandId}"
                );
            }

            try
            {
                await _defaultRetry.ExecuteAsync(
                    async ct =>
                    {
                        if (isSuccess)
                        {
                            await _eventQueue.Complete(value, ct);

                            _logger.ZLogDebug($"Completed result {value.CommandId}");
                        }
                        else
                        {
                            await _eventQueue.Fail(value, ct);

                            _logger.ZLogDebug($"Marked result {value.CommandId} as failed");
                        }
                    },
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _logger.ZLogError(ex, $"An error occurred while updating result {value.CommandId}");
            }
        }
    }
}
