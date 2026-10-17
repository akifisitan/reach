using System.Net.Http.Headers;
using System.Text;

namespace Reach.Relay.Confluence;

public interface IConfluenceHttpClientFactory
{
    HttpClient CreateClient();
    HttpClient CreateClient(ConfluenceUserCredential credential);
    HttpClient CreateClient(ConfluenceUserCredential credential, string baseUrl);
}

public sealed class ConfluenceHttpClientFactory : IConfluenceHttpClientFactory
{
    private readonly IConfluenceUserCredentialProvider _confluenceUserCredentialsProvider;
    private readonly ConfluenceConfigurationStore _store;
    private ConfluenceOptions Options => _store.Current;

    public ConfluenceHttpClientFactory(
        IConfluenceUserCredentialProvider confluenceUserCredentialsProvider,
        ConfluenceConfigurationStore configurationStore
    )
    {
        _confluenceUserCredentialsProvider = confluenceUserCredentialsProvider;
        _store = configurationStore;
    }

    private static readonly SocketsHttpHandler httpHandler = new()
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
    };

    private readonly Lock _authHeaderLock = new();
    private string? _cachedCredentialKey;
    private AuthenticationHeaderValue? _cachedAuthenticationHeaderValue;

    public HttpClient CreateClient() =>
        CreateClient(_confluenceUserCredentialsProvider.GetCredential(), Options.BaseUrl);

    public HttpClient CreateClient(ConfluenceUserCredential credential) =>
        CreateClient(credential, Options.BaseUrl);

    public HttpClient CreateClient(ConfluenceUserCredential credential, string baseUrl)
    {
        var httpClient = new HttpClient(httpHandler, disposeHandler: false)
        {
            BaseAddress = new Uri(baseUrl),
        };

        httpClient.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderValue(credential);

        return httpClient;
    }

    private AuthenticationHeaderValue GetAuthenticationHeaderValue(
        ConfluenceUserCredential credential
    )
    {
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
