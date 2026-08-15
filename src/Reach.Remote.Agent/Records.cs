namespace Reach.Remote.Agent;

internal sealed record AppOptions
{
    public const string SectionName = "AppOptions";

    public bool DebugMode { get; set; }
}

internal sealed record ExecutorOptions
{
    public int ProcessorCount { get; set; }
    public int RetryConsumerCount { get; set; }
    public int PosterCount { get; set; }
}
