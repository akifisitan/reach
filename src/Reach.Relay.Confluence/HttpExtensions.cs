using System.Net;

namespace Reach.Relay.Confluence;

internal static class HttpExtensions
{
    public static bool IsAuthError(this Exception ex) =>
        ex
            is HttpRequestException
            {
                StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            };
}
