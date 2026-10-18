namespace Reach.Module.SoapSearch.Discovery;

// Path construction copied from Serara.Module.WinServer.WinApplicationPathProvider.
internal sealed class WinApplicationPathProvider
{
    private readonly SoapSearchOptions _options;

    public WinApplicationPathProvider(SoapSearchOptions options)
    {
        _options = options;
    }

    public string GetLiveServerLogPath(WinApplicationMetadata metadata, string serverName) =>
        Path.Combine(
            _options.LiveServerBasePath,
            serverName,
            _options.LogPathPrefix,
            metadata.LogPath
        );

    public string GetArchiveServerLogPath(WinApplicationMetadata metadata, string serverName) =>
        Path.Combine(
            _options.ArchiveServerBasePath,
            metadata.ArchiveDirectoryName,
            serverName.ToUpperInvariant(),
            _options.LogPathPrefix,
            metadata.LogPath
        );
}
