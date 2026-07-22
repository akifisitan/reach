namespace Reach.Local.Server;

internal static class AppPaths
{
    public static readonly string AppDataLocalBasePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppConstants.AppPackageName,
        "local"
    );

    public static readonly string LogFilePath = Path.Combine(
        AppDataLocalBasePath,
        "logs",
        $"{DateTime.Now:yyyy-MM-dd}.log"
    );

    public static readonly string LockFilePath = Path.Combine(
        AppDataLocalBasePath,
        $"{AppConstants.AppName}.local.server.lock"
    );
}
