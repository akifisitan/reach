using System.Net.Http.Headers;
using System.Text;

namespace Reach.Relay.Confluence;

public interface IConfluenceHttpClientFactory
{
    HttpClient CreateClient();
}

public sealed class ConfluenceHttpClientFactory : IConfluenceHttpClientFactory
{
    private readonly IConfluenceUserCredentialProvider _confluenceUserCredentialsProvider;
    private readonly ConfluenceOptions _options;

    public ConfluenceHttpClientFactory(
        IConfluenceUserCredentialProvider confluenceUserCredentialsProvider,
        IOptions<ConfluenceOptions> options
    )
    {
        _confluenceUserCredentialsProvider = confluenceUserCredentialsProvider;
        _options = options.Value;
    }

    private static readonly SocketsHttpHandler httpHandler = new()
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
    };

    private readonly Lock _authHeaderLock = new();
    private string? _cachedCredentialKey;
    private AuthenticationHeaderValue? _cachedAuthenticationHeaderValue;

    public HttpClient CreateClient()
    {
        return CreateHttpClient();
    }

    private HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient(httpHandler, disposeHandler: false)
        {
            BaseAddress = new Uri(_options.BaseUrl),
        };

        httpClient.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderValue();

        return httpClient;
    }

    private AuthenticationHeaderValue GetAuthenticationHeaderValue()
    {
        var credential = _confluenceUserCredentialsProvider.GetCredential();

        lock (_authHeaderLock)
        {
            var credentialKey = credential switch
            {
                ConfluencePersonalAccessTokenCredential accessTokenCredential =>
                    $"pat:{accessTokenCredential.AccessToken}",
                ConfluenceBasicAuthCredential basicAuthCredential =>
                    $"basic:{basicAuthCredential.Username}:{basicAuthCredential.Password}",
                _ => throw new InvalidOperationException(
                    $"Invalid {nameof(ConfluenceUserCredential)} type"
                ),
            };

            if (
                _cachedCredentialKey == credentialKey
                && _cachedAuthenticationHeaderValue is not null
            )
            {
                return _cachedAuthenticationHeaderValue;
            }

            _cachedAuthenticationHeaderValue = credential switch
            {
                ConfluencePersonalAccessTokenCredential accessTokenCredential => new(
                    "Bearer",
                    accessTokenCredential.AccessToken
                ),
                ConfluenceBasicAuthCredential basicAuthCredential => new(
                    "Basic",
                    Convert.ToBase64String(
                        Encoding.UTF8.GetBytes(
                            $"{basicAuthCredential.Username}:{basicAuthCredential.Password}"
                        )
                    )
                ),
                _ => throw new InvalidOperationException(
                    $"Invalid {nameof(ConfluenceUserCredential)} type"
                ),
            };
            _cachedCredentialKey = credentialKey;

            return _cachedAuthenticationHeaderValue;
        }
    }
}
