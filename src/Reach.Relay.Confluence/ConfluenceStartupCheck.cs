namespace Reach.Relay.Confluence;

/// <summary>
/// Verifies the Confluence relay configuration against the live API at startup.
/// Configuration errors (unknown pages, bad credentials) trigger a setup re-run;
/// transient connectivity problems only log a warning since the relay retries
/// in the background.
/// </summary>
public sealed class ConfluenceStartupCheck : IStartupCheck
{
    private readonly IConfluenceHttpClientFactory _httpClientFactory;
    private readonly ConfluenceOptions _options;
    private readonly ILogger<ConfluenceStartupCheck> _logger;

    public ConfluenceStartupCheck(
        IConfluenceHttpClientFactory httpClientFactory,
        IOptions<ConfluenceOptions> options,
        ILogger<ConfluenceStartupCheck> logger
    )
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task Check(CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();

            httpClient.Timeout = TimeSpan.FromSeconds(5);

            await ConfluenceConfigurationCheck
                .CheckAsync(
                    httpClient,
                    _options.LocalToRemotePageId,
                    _options.RemoteToLocalPageId,
                    cancellationToken
                )
                .ConfigureAwait(false);

            _logger.ZLogInformation($"Confluence relay configuration verified");
        }
        catch (HttpRequestException ex) when (ConfluenceConfigurationCheck.IsConfigurationError(ex))
        {
            throw new RelaySetupRequiredException(
                $"Confluence relay is misconfigured ({(int)ex.StatusCode!} {ex.StatusCode}).",
                ex
            );
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.ZLogWarning(
                ex,
                $"Confluence is unreachable at startup. The relay will keep retrying in the background."
            );
        }
    }
}
