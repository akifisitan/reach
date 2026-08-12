using Polly;
using Polly.Registry;
using Reach.Hosting;
using Reach.Infra.CommandResultOutbox;
using Reach.Infra.DurableQueue;
using Reach.Module;
using Reach.Relay;

namespace Reach.Remote.Agent;

internal sealed class CommandQueueProcessor
{
    private readonly ILogger<CommandQueueProcessor> _logger;
    private readonly IDurableQueue<RelayCommand> _commandQueue;
    private readonly IHandlerDispatcher<RelayCommand, RelayCommandResult> _handler;
    private readonly CommandResultOutbox _commandResultOutbox;
    private readonly ResiliencePipeline _retryPipeline;

    public CommandQueueProcessor(
        ILogger<CommandQueueProcessor> logger,
        IDurableQueue<RelayCommand> commandQueue,
        IHandlerDispatcher<RelayCommand, RelayCommandResult> handler,
        CommandResultOutbox commandResultOutbox,
        ResiliencePipelineProvider<string> resiliencePipelineProvider
    )
    {
        _logger = logger;
        _commandQueue = commandQueue;
        _handler = handler;
        _commandResultOutbox = commandResultOutbox;
        _retryPipeline = resiliencePipelineProvider.GetPipeline(Constants.DefaultRetryPolicy);
    }

    public async Task Start(bool isForRetry, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            RelayCommand? command;

            try
            {
                command = isForRetry
                    ? await _commandQueue.PickForRetry(cancellationToken)
                    : await _commandQueue.Pick(cancellationToken);

                if (command is null)
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

            RelayCommandResult? commandResult = null;

            try
            {
                commandResult = await _handler.Handle(command, cancellationToken);

                _logger.ZLogDebug($"Handled command {command.Id}");
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(ex, $"An error occurred while handling command {command.Id}");
            }

            try
            {
                await _retryPipeline.ExecuteAsync(
                    async ct =>
                    {
                        if (commandResult is not null)
                        {
                            await _commandResultOutbox.CompleteCommandWithResult(
                                command,
                                commandResult,
                                ct
                            );

                            _logger.ZLogDebug($"Completed command {command.Id} with result");
                        }
                        else
                        {
                            await _commandQueue.Fail(command, ct);

                            _logger.ZLogDebug($"Marked command {command.Id} as failed");
                        }
                    },
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _logger.ZLogError(ex, $"An error occurred while updating command {command.Id}");
            }
        }
    }
}
