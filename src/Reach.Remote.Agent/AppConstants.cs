using System.Diagnostics;

namespace Reach.Remote.Agent;

internal static class AppConstants
{
#if DEBUG
    public const string AppPackageName = "Reach.Debug";
#else
    public const string AppPackageName = "Reach";
#endif

    public const string AppName = "Reach";

    public static readonly string AppVersion =
        FileVersionInfo.GetVersionInfo(Environment.ProcessPath!).ProductVersion ?? "Unknown";
}
