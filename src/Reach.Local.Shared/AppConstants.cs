using System.Diagnostics;

namespace Reach.Local.Shared;

internal static class AppConstants
{
#if DEBUG
    public const string AppPackageName = "Reach.Debug";
#else
    public const string AppPackageName = "Reach";
#endif

    public static readonly string AppVersion =
        FileVersionInfo.GetVersionInfo(Environment.ProcessPath!).ProductVersion ?? "Unknown";
}
