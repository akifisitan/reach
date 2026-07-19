namespace Reach.Local.Server;

internal sealed record AppOptions
{
    public bool DebugMode { get; set; }
}

internal sealed record ServerExecutorOptions
{
    public int ConsumerCount { get; set; }
    public int RetryConsumerCount { get; set; }
}
