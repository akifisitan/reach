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
        using var response = await httpClient
            .GetAsync("confluence/rest/api/user/current", cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        await Task.WhenAll(
                EnsurePageExists(httpClient, localToRemotePageId, cancellationToken),
                EnsurePageExists(httpClient, remoteToLocalPageId, cancellationToken)
            )
            .ConfigureAwait(false);
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
