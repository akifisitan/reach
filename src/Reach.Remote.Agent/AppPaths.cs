using Reach.Composition;

namespace Reach.Remote.Agent;

internal static class AppPaths
{
    public static readonly string AppDataLocalBasePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppConstants.AppPackageName,
        "remote"
    );

    public static readonly string LogFilePath = Path.Combine(
        AppDataLocalBasePath,
        "logs",
        $"{DateTime.Now:yyyy-MM-dd}.log"
    );

    public static readonly string LockFilePath = Path.Combine(
        AppDataLocalBasePath,
        $"{Constants.AppName}.remote.server.lock"
    );
}
