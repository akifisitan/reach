namespace Reach.Hosting;

public sealed record ReachLoggingOptions
{
    public const string SectionName = "ReachLoggingOptions";

    public bool OpenLogFileOnStart { get; set; }
}
