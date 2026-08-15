namespace Reach.Hosting;

public interface ISingleAppInstanceLock : IDisposable
{
    bool TryAcquire(string path);
}

internal sealed class SingleAppInstanceLock : ISingleAppInstanceLock, IDisposable
{
    private readonly ILogger<SingleAppInstanceLock> _logger;

    private string? _lockFilePath;
    private FileStream? _lockFile;
    private bool _disposed;

    public SingleAppInstanceLock(ILogger<SingleAppInstanceLock> logger)
    {
        _logger = logger;
    }

    public bool TryAcquire(string path)
    {
        try
        {
            _lockFilePath = path;

            _lockFile = new FileStream(
                path,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None
            );

            _lockFile.SetLength(0);

            using var writer = new StreamWriter(_lockFile, leaveOpen: true);

            writer.Write(
                $"""
                {Environment.MachineName}
                {Environment.ProcessId}
                {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff}
                """
            );

            writer.Flush();

            _lockFile.Position = 0;

            return true;
        }
        catch (IOException ex)
        {
            _logger.ZLogError(
                ex,
                $"An error occurred while trying to open lockfile. This might mean another process is already running"
            );

            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _lockFile?.Dispose();

        try
        {
            if (!string.IsNullOrWhiteSpace(_lockFilePath))
            {
                File.Delete(_lockFilePath);
            }
        }
        catch { }

        _disposed = true;
    }
}
