namespace Reach.Remote.Agent;

internal sealed record AppOptions
{
    public bool DebugMode { get; set; }
}

internal sealed record ExecutorOptions
{
    public int ConsumerCount { get; set; }
}
