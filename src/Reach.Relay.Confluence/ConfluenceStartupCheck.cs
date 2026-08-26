using System.Diagnostics;
using System.Security.Cryptography;

namespace Reach.Relay.Confluence;

public sealed class ConfluenceStartupCheck : IStartupCheck
{
    private static readonly TimeSpan RetryDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    private readonly IConfluenceHttpClientFactory _httpClientFactory;
    private readonly ConfluenceConfigurationStore _configurationStore;
    private readonly ILogger<ConfluenceStartupCheck> _logger;

    public ConfluenceStartupCheck(
        IConfluenceHttpClientFactory httpClientFactory,
        ConfluenceConfigurationStore configurationStore,
        ILogger<ConfluenceStartupCheck> logger
    )
    {
        _httpClientFactory = httpClientFactory;
        _configurationStore = configurationStore;
        _logger = logger;
    }

    public async Task Check(CancellationToken cancellationToken)
    {
        if (!_configurationStore.TryLoad(out var errorMessage))
        {
            throw new RelayConfigurationException(errorMessage);
        }

        var options = _configurationStore.Current;
        var stopwatch = Stopwatch.StartNew();
        Exception? lastError = null;

        while (true)
        {
            var remaining = RetryDuration - stopwatch.Elapsed;

            if (remaining <= TimeSpan.Zero)
            {
                throw new RelayUnavailableException(
                    "Confluence did not become available within 30 seconds.",
                    lastError!
                );
            }

            try
            {
                using var httpClient = _httpClientFactory.CreateClient();

                var requestTimeout = TimeSpan.FromSeconds(5);
                httpClient.Timeout = remaining < requestTimeout ? remaining : requestTimeout;

                await ConfluenceConfigurationCheck
                    .CheckAsync(
                        httpClient,
                        options.LocalToRemotePageId,
                        options.RemoteToLocalPageId,
                        cancellationToken
                    )
                    .ConfigureAwait(false);

                _logger.ZLogInformation($"Confluence relay configuration verified");
                return;
            }
            catch (HttpRequestException ex) when (ex.IsConfigurationError())
            {
                throw new RelaySetupRequiredException(
                    $"Confluence rejected the configuration ({(int)ex.StatusCode!} {ex.StatusCode}).",
                    ex
                );
            }
            catch (Exception ex)
                when (ex
                        is CryptographicException
                            or IOException
                            or InvalidOperationException
                            or UnauthorizedAccessException
                )
            {
                throw new RelayConfigurationException(
                    "The Confluence credential is missing or invalid.",
                    ex
                );
            }
            catch (Exception ex)
                when (ex is HttpRequestException or TaskCanceledException
                    && !cancellationToken.IsCancellationRequested
                )
            {
                lastError = ex;
                _logger.ZLogWarning(ex, $"Confluence is unavailable; retrying startup check");

                remaining = RetryDuration - stopwatch.Elapsed;

                if (remaining > TimeSpan.Zero)
                {
                    var delay = remaining < RetryDelay ? remaining : RetryDelay;
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
