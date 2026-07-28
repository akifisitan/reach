namespace Reach.Hosting;

public static class Constants
{
    public const string AppName = "Reach";
    public const string DefaultRetryPolicy = "default-retry";

#if DEBUG
    public const string AppPackageName = "Reach.Debug";
#else
    public const string AppPackageName = "Reach";
#endif
}
