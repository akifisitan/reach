using Polly;
using Polly.Simmy;

namespace Reach.Hosting;

public static class ChaosPipelineBuilderExtensions
{
    public static ResiliencePipelineBuilder AddConfiguredChaosFault(
        this ResiliencePipelineBuilder builder,
        ChaosOptions chaosOptions,
        ChaosPointOptions pointOptions,
        string pointName
    )
    {
        if (!chaosOptions.Enabled || !pointOptions.Enabled || pointOptions.InjectionRate <= 0)
        {
            return builder;
        }

        return builder.AddChaosFault(
            pointOptions.InjectionRate,
            () => new ChaosFaultException(pointName)
        );
    }

    public static ResiliencePipelineBuilder AddConfiguredChaosLatency(
        this ResiliencePipelineBuilder builder,
        ChaosOptions chaosOptions,
        ChaosPollOptions pointOptions
    )
    {
        if (
            !chaosOptions.Enabled
            || !pointOptions.LatencyEnabled
            || pointOptions.LatencyRate <= 0
            || pointOptions.Latency <= TimeSpan.Zero
        )
        {
            return builder;
        }

        return builder.AddChaosLatency(pointOptions.LatencyRate, pointOptions.Latency);
    }
}
