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
        Create("local", $"{Constants.AppName}.local.server.lock");

    public static ReachAppPaths RemoteAgent() =>
        Create("remote", $"{Constants.AppName}.remote.server.lock");

    public static ReachAppPaths LocalCli() => Create("cli", lockFileName: null);

    private static ReachAppPaths Create(string profile, string? lockFileName)
    {
        var basePath = Path.Combine(SharedPath, profile);

        return new()
        {
            AppDataBasePath = basePath,
            LogFilePath = Path.Combine(basePath, "logs", $"{DateTime.Now:yyyy-MM-dd}.log"),
            LockFilePath = lockFileName is null ? null : Path.Combine(basePath, lockFileName),
        };
    }
}
