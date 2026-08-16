using Polly;
using Polly.Registry;
using Reach.Relay;

namespace Reach.Module.Toolkit.Resilience;

public sealed class ResilientRelayPoster
{
    private readonly ILocalRelay _localRelay;
    private readonly ResiliencePipeline _retryPipeline;
    private readonly ResiliencePipeline _chaosPipeline;

    public ResilientRelayPoster(
        ILocalRelay localRelay,
        ResiliencePipelineProvider<string> resiliencePipelineProvider
    )
    {
        _localRelay = localRelay;
        _retryPipeline = resiliencePipelineProvider.GetDefaultRetryPipeline();
        _chaosPipeline = resiliencePipelineProvider.GetDefaultRetryPipeline();
    }

    public async Task<PostToRemoteResult> PostToRemote(
        RelayCommand command,
        CancellationToken cancellationToken
    )
    {
        return await _retryPipeline.ExecuteAsync(
            async ct => await _localRelay.PostToRemote(command, ct),
            cancellationToken
        );
    }
}
