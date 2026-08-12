namespace Reach.Hosting;

public sealed record ReachAppPaths
{
    public required string AppDataBasePath { get; init; }
    public required string LogFilePath { get; init; }
    public string? LockFilePath { get; init; }

    public static readonly string SharedPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        Constants.AppPackageName
    );

    public static ReachAppPaths LocalServer() =>
        Create("local", lockFileName: $"{Constants.AppName}.local.server.lock");

    public static ReachAppPaths RemoteAgent() =>
        Create("remote", lockFileName: $"{Constants.AppName}.remote.server.lock");

    public static ReachAppPaths LocalCli() =>
        Create("cli", logFileName: $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}.log");

    private static ReachAppPaths Create(
        string profile,
        string? logFileName = null,
        string? lockFileName = null
    )
    {
        var basePath = Path.Combine(SharedPath, profile);
        logFileName ??= $"{DateTime.Now:yyyy-MM-dd}.log";

        return new()
        {
            AppDataBasePath = basePath,
            LogFilePath = Path.Combine(basePath, "logs", logFileName),
            LockFilePath = lockFileName is null ? null : Path.Combine(basePath, lockFileName),
        };
    }
}
