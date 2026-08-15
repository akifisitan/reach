namespace Reach.Local.Server;

internal sealed record AppOptions
{
    public bool DebugMode { get; set; }
}

internal sealed record ServerExecutorOptions
{
    public required int QueueProcessorCount { get; set; }
    public required int RetryQueueProcessorCount { get; set; }
}
