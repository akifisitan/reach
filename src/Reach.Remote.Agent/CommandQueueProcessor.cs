using Polly;
using Polly.Registry;
using Reach.Hosting;
using Reach.Infra.DurableQueue;
using Reach.Module;
using Reach.Relay;

namespace Reach.Remote.Agent;

internal sealed class CommandQueueProcessor
{
    private readonly ILogger<CommandQueueProcessor> _logger;
    private readonly IDurableQueue<RelayCommand> _commandQueue;
    private readonly IRemoteRelay _relay;
    private readonly IHandlerDispatcher<RelayCommand, RelayCommandResult> _handler;
    private readonly ResiliencePipeline _retryPipeline;

    public CommandQueueProcessor(
        ILogger<CommandQueueProcessor> logger,
        IDurableQueue<RelayCommand> commandQueue,
        IRemoteRelay relay,
        IHandlerDispatcher<RelayCommand, RelayCommandResult> handler,
        ResiliencePipelineProvider<string> resiliencePipelineProvider
    )
    {
        _logger = logger;
        _commandQueue = commandQueue;
        _relay = relay;
        _handler = handler;
        _retryPipeline = resiliencePipelineProvider.GetPipeline(Constants.DefaultRetryPolicy);
    }

    public async Task Start(bool isForRetry, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RelayCommand? value;

            try
            {
                value = isForRetry
                    ? await _commandQueue.PickForRetry(cancellationToken)
                    : await _commandQueue.Pick(cancellationToken);

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
                // TODO: Handle is mostly a costly operation, so maybe we store the result as well and post to local from a separate workflow?
                var commandResult = await _handler.Handle(value, cancellationToken);

                await _relay.PostToLocal(commandResult, cancellationToken);

                isSuccess = true;
            }
            catch (Exception ex)
            {
                _logger.ZLogError(ex, $"An error occurred while handling");
            }

            try
            {
                await _retryPipeline.ExecuteAsync(
                    async ct =>
                    {
                        if (isSuccess)
                        {
                            await _commandQueue.Complete(value, ct);
                        }
                        else
                        {
                            await _commandQueue.Fail(value, ct);
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
