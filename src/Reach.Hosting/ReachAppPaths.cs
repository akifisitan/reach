namespace Reach.Hosting;

public sealed record ReachAppPaths(string AppDataBasePath, string LogFilePath, string? LockFilePath)
{
    public static ReachAppPaths LocalServer() =>
        Create("local", $"{Constants.AppName}.local.server.lock");

    public static ReachAppPaths RemoteAgent() =>
        Create("remote", $"{Constants.AppName}.remote.server.lock");

    public static ReachAppPaths LocalCli() => Create("cli", lockFileName: null);

    private static ReachAppPaths Create(string profile, string? lockFileName)
    {
        var basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Constants.AppPackageName,
            profile
        );

        return new(
            basePath,
            Path.Combine(basePath, "logs", $"{DateTime.Now:yyyy-MM-dd}.log"),
            lockFileName is null ? null : Path.Combine(basePath, lockFileName)
        );
    }
}
