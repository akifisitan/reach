namespace Reach.DurableQueue;

public sealed record DurableQueueOptions
{
    public int MaxTryCount { get; set; } = 4;
    public required string TableName { get; set; }
}
