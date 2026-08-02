using Polly;
using Polly.Registry;
using Reach.Hosting;
using Reach.Infra.CommandResultOutbox;
using Reach.Relay;

namespace Reach.Remote.Agent;

internal sealed class CommandResultPoster
{
    private readonly ILogger<CommandResultPoster> _logger;
    private readonly CommandResultOutbox _commandResultOutbox;
    private readonly IRemoteRelay _relay;
    private readonly ResiliencePipeline _retryPipeline;

    public CommandResultPoster(
        ILogger<CommandResultPoster> logger,
        IRemoteRelay relay,
        CommandResultOutbox commandResultOutbox,
        ResiliencePipelineProvider<string> resiliencePipelineProvider
    )
    {
        _logger = logger;
        _relay = relay;
        _commandResultOutbox = commandResultOutbox;
        _retryPipeline = resiliencePipelineProvider.GetPipeline(Constants.DefaultRetryPolicy);
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            CommandResultOutboxItem? item;

            try
            {
                item = await _commandResultOutbox.PickPending(cancellationToken);

                if (item is null)
                {
                    await Task.Delay(Random.Shared.Next(1500, 1700), cancellationToken);

                    continue;
                }
            }
            catch (Exception ex)
            {
                _logger.ZLogError(ex, $"An error occurred while picking an outbox result");

                await Task.Delay(Random.Shared.Next(2050, 2200), cancellationToken);

                continue;
            }

            try
            {
                await _relay.PostToLocal(item.Result, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.ZLogError(
                    ex,
                    $"An error occurred while posting result {item.CommandId} to local, will retry"
                );

                try
                {
                    await _commandResultOutbox.MarkPendingForRetry(
                        item.CommandId,
                        cancellationToken
                    );
                }
                catch (Exception updateEx)
                {
                    _logger.ZLogError(
                        updateEx,
                        $"An error occurred while scheduling result {item.CommandId} for retry"
                    );
                }

                continue;
            }

            try
            {
                await _retryPipeline.ExecuteAsync(
                    async ct =>
                    {
                        await _commandResultOutbox.MarkPosted(item.CommandId, ct);
                    },
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _logger.ZLogError(ex, $"An error occurred while marking result as posted");
            }
        }
    }
}
