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

    public static bool IsConfigurationError(this HttpRequestException ex) =>
        ex.StatusCode
            is HttpStatusCode.Unauthorized
                or HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound;
}
