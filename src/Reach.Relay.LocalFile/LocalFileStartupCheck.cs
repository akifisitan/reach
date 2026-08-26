namespace Reach.Relay.LocalFile;

public sealed class LocalFileStartupCheck : IStartupCheck
{
    private readonly LocalFileConfigurationStore _configurationStore;
    private readonly ILogger<LocalFileStartupCheck> _logger;

    public LocalFileStartupCheck(
        LocalFileConfigurationStore configurationStore,
        ILogger<LocalFileStartupCheck> logger
    )
    {
        _configurationStore = configurationStore;
        _logger = logger;
    }

    public Task Check(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_configurationStore.TryLoad(out var errorMessage))
        {
            throw new RelayConfigurationException(errorMessage);
        }

        var options = _configurationStore.Current;

        try
        {
            CheckFile(options.LocalToRemoteFilePath);
            CheckFile(options.RemoteToLocalFilePath);
        }
        catch (FileNotFoundException ex)
        {
            throw new RelaySetupRequiredException(
                "A local relay file is missing. Run relay setup again.",
                ex
            );
        }
        catch (Exception ex)
            when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            throw new RelayUnavailableException("A local relay file is unavailable.", ex);
        }

        _logger.ZLogInformation($"Local-file relay configuration verified");
        return Task.CompletedTask;
    }

    private static void CheckFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Relay file not found.", filePath);
        }

        using (File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) { }
        using (File.Open(filePath, FileMode.Append, FileAccess.Write, FileShare.Read)) { }
    }
}
