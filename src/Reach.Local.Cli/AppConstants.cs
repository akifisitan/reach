using System.Diagnostics;

namespace Reach.Local.Cli;

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

    public const string ShouldNotBootServerEnvVar = "REACH_SHOULD_NOT_BOOT_SERVER";
}
