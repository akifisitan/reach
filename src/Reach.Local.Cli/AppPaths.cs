namespace Reach.Local.Cli;

internal static class AppPaths
{
    public static readonly string AppDataLocalBasePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppConstants.AppPackageName,
        "cli"
    );

    public static readonly string LogFilePath = Path.Combine(
        AppDataLocalBasePath,
        "logs",
        $"{DateTime.Now:yyyy-MM-dd}.log"
    );

    public static readonly string ServerBinaryPath = Path.Combine(
        Environment.CurrentDirectory,
        "reachd.exe"
    );
}
