using System.Net;

namespace Reach.Relay.Confluence;

internal static class ConfluenceConfigurationCheck
{
    public static async Task CheckAsync(
        HttpClient httpClient,
        long localToRemotePageId,
        long remoteToLocalPageId,
        CancellationToken cancellationToken
    )
    {
        using var currentUserResponse = await httpClient
            .GetAsync("confluence/rest/api/user/current", cancellationToken)
            .ConfigureAwait(false);

        currentUserResponse.EnsureSuccessStatusCode();

        await EnsurePageExists(httpClient, localToRemotePageId, cancellationToken)
            .ConfigureAwait(false);
        await EnsurePageExists(httpClient, remoteToLocalPageId, cancellationToken)
            .ConfigureAwait(false);
    }

    public static bool IsConfigurationError(HttpRequestException ex)
    {
        return ex.StatusCode
            is HttpStatusCode.Unauthorized
                or HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound;
    }

    private static async Task EnsurePageExists(
        HttpClient httpClient,
        long pageId,
        CancellationToken cancellationToken
    )
    {
        using var response = await httpClient
            .GetAsync($"confluence/rest/api/content/{pageId}", cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
    }
}
