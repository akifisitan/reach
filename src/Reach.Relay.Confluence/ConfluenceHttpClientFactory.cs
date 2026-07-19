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

    private static string? _cachedAuthHeaderContent;
    private static AuthenticationHeaderValue? _cachedAuthenticationHeaderValue;

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

        if (credential is ConfluencePersonalAccessTokenCredential accessTokenCredential)
        {
            _cachedAuthenticationHeaderValue ??= new("Bearer", accessTokenCredential.AccessToken);

            return _cachedAuthenticationHeaderValue;
        }

        if (credential is not ConfluenceBasicAuthCredential basicAuthCredential)
        {
            throw new InvalidOperationException($"Invalid {nameof(ConfluenceUserCredential)} type");
        }

        var kvp = $"{basicAuthCredential.Username}:{basicAuthCredential.Password}";

        if (_cachedAuthHeaderContent == kvp)
        {
            ArgumentNullException.ThrowIfNull(_cachedAuthenticationHeaderValue);

            return _cachedAuthenticationHeaderValue;
        }

        _cachedAuthenticationHeaderValue = new(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes(kvp))
        );
        _cachedAuthHeaderContent = kvp;

        return _cachedAuthenticationHeaderValue;
    }
}
