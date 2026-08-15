namespace Reach.Hosting;

public static class Constants
{
    public const string AppName = "Reach";

#if DEBUG
    public const string AppPackageName = "Reach.Debug";
#else
    public const string AppPackageName = "Reach";
#endif
}

public static class RetryPolicyNames
{
    public const string Default = "default-retry";
}
