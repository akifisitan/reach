using Polly;
using Polly.Registry;

namespace Reach.Module.Toolkit.Resilience;

public static class ResiliencePipelineProviderExtensions
{
    public static ResiliencePipeline GetDefaultRetryPipeline(
        this ResiliencePipelineProvider<string> resiliencePipelineProvider
    ) => resiliencePipelineProvider.GetPipeline(ModuleResiliencePolicyNames.DefaultRetry);
}
