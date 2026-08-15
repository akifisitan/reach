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
    private readonly ResiliencePipeline _chaosPipeline;

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
        _retryPipeline = resiliencePipelineProvider.GetPipeline(RetryPolicyNames.Default);
        _chaosPipeline = resiliencePipelineProvider.GetPipeline(ChaosPolicyNames.RelayPost);
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var item = await PickFromOutbox(cancellationToken);

            if (item is null)
            {
                continue;
            }

            var isSuccess = await PostToLocal(item, cancellationToken);

            await MarkAsPosted(isSuccess, item, cancellationToken);
        }
    }

    private async Task<CommandResultOutboxItem?> PickFromOutbox(CancellationToken cancellationToken)
    {
        try
        {
            var item = await _commandResultOutbox.PickPending(cancellationToken);

            if (item is null)
            {
                await Task.Delay(Random.Shared.Next(1500, 1700), cancellationToken);

                return null;
            }

            return item;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"An error occurred while picking an outbox result");

            await Task.Delay(Random.Shared.Next(2050, 2200), cancellationToken);

            return null;
        }
    }

    private async Task<bool> PostToLocal(
        CommandResultOutboxItem item,
        CancellationToken cancellationToken
    )
    {
        try
        {
            _logger.ZLogDebug($"Posting result {item.CommandId} to local");

            await _chaosPipeline.ExecuteAsync(
                async ct => await _relay.PostToLocal(item.Result, ct),
                cancellationToken
            );

            _logger.ZLogInformation($"Posted result {item.CommandId} to local relay");

            return true;
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(
                ex,
                $"An error occurred while posting result {item.CommandId} to local, will retry"
            );

            return false;
        }
    }

    private async Task MarkAsPosted(
        bool isSuccess,
        CommandResultOutboxItem item,
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
                        await _commandResultOutbox.MarkPosted(item.CommandId, ct);

                        _logger.ZLogDebug($"Updating result {item.CommandId} as posted");
                    }
                    else
                    {
                        _logger.ZLogDebug($"Updating result {item.CommandId} for retry");

                        await _commandResultOutbox.MarkPendingForRetry(
                            item.CommandId,
                            cancellationToken
                        );
                    }
                },
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"An error occurred while updating result");
        }
    }
}
