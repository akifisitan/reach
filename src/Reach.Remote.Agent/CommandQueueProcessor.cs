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
            var command = await PickFromQueue(isForRetry, cancellationToken);

            if (command is null)
            {
                continue;
            }

            var result = await ProcessCommand(command, cancellationToken);

            await MarkAsComplete(command, result, cancellationToken);
        }
    }

    private async Task<RelayCommand?> PickFromQueue(
        bool isForRetry,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var command = isForRetry
                ? await _commandQueue.PickForRetry(cancellationToken)
                : await _commandQueue.Pick(cancellationToken);

            if (command is null)
            {
                await Task.Delay(Random.Shared.Next(1500, 1700), cancellationToken);

                return null;
            }

            return command;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"An error occurred while picking from the queue");

            await Task.Delay(Random.Shared.Next(2050, 2200), cancellationToken);

            return null;
        }
    }

    private async Task<RelayCommandResult?> ProcessCommand(
        RelayCommand command,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var commandResult = await _handler.Handle(command, cancellationToken);

            _logger.ZLogDebug($"Handled command {command.Id}");

            return commandResult;
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"An error occurred while handling command {command.Id}");

            return null;
        }
    }

    private async Task MarkAsComplete(
        RelayCommand command,
        RelayCommandResult? commandResult,
        CancellationToken cancellationToken
    )
    {
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

                        _logger.ZLogDebug($"Updated command {command.Id} as complete");
                    }
                    else
                    {
                        await _commandQueue.Fail(command, ct);

                        _logger.ZLogDebug($"Updated command {command.Id} as failed");
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
