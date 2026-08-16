using Polly;
using Polly.Registry;
using Reach.Hosting;
using Reach.Infra.DurableQueue;
using Reach.Module;
using Reach.Relay;

namespace Reach.Local.Server;

internal sealed class RelayCommandResultQueueProcessor
{
    private readonly ILogger<RelayCommandResultQueueProcessor> _logger;
    private readonly IDurableQueue<RelayCommandResult> _queue;
    private readonly IHandlerDispatcher<RelayCommandResult, Unit> _handlerDispatcher;
    private readonly ResiliencePipeline _retryPipeline;
    private readonly ResiliencePipeline _chaosPipeline;

    public RelayCommandResultQueueProcessor(
        ILogger<RelayCommandResultQueueProcessor> logger,
        IDurableQueue<RelayCommandResult> queue,
        IHandlerDispatcher<RelayCommandResult, Unit> handlerDispatcher,
        ResiliencePipelineProvider<string> resiliencePipelineProvider
    )
    {
        _logger = logger;
        _queue = queue;
        _handlerDispatcher = handlerDispatcher;
        _retryPipeline = resiliencePipelineProvider.GetPipeline(RetryPolicyNames.Default);
        _chaosPipeline = resiliencePipelineProvider.GetPipeline(ChaosPolicyNames.RelayResult);
    }

    public async Task Start(bool isForRetry, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var value = await PickFromQueue(isForRetry, cancellationToken);

            if (value is null)
            {
                continue;
            }

            var isSuccess = await Process(value, cancellationToken);

            await UpdateResult(isSuccess, value, cancellationToken);
        }
    }

    private async Task<RelayCommandResult?> PickFromQueue(
        bool isForRetry,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var value = isForRetry
                ? await _queue.PickForRetry(cancellationToken)
                : await _queue.Pick(cancellationToken);

            if (value is null)
            {
                await Task.Delay(Random.Shared.Next(1500, 1700), cancellationToken);

                return null;
            }

            _logger.ZLogDebug($"Picked value {value} from the queue");

            return value;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"An error occurred while picking from the queue");

            await Task.Delay(Random.Shared.Next(2050, 2200), cancellationToken);

            return null;
        }
    }

    private async Task<bool> Process(RelayCommandResult value, CancellationToken cancellationToken)
    {
        try
        {
            await _chaosPipeline.ExecuteAsync(
                async ct => await _handlerDispatcher.Handle(value, ct),
                cancellationToken
            );

            _logger.ZLogDebug($"Processed result successfully {value.CommandId}");

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"An error occurred while processing result {value.CommandId}");

            return false;
        }
    }

    private async Task UpdateResult(
        bool isSuccess,
        RelayCommandResult value,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await _retryPipeline.ExecuteAsync(
                async ct =>
                {
                    if (isSuccess)
                    {
                        await _queue.Complete(value, ct);

                        _logger.ZLogDebug($"Updated result {value.CommandId} as complete");
                    }
                    else
                    {
                        await _queue.Fail(value, ct);

                        _logger.ZLogDebug($"Updated result {value.CommandId} as failed");
                    }
                },
                cancellationToken
            );
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"An error occurred while updating result {value.CommandId}");
        }
    }
}
