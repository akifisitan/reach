using Reach.Relay.Confluence;

namespace Reach.Shared;

internal sealed class ConfluenceUserCredentialProvider : IConfluenceUserCredentialProvider
{
    public ConfluenceUserCredential GetCredential() =>
        new ConfluencePersonalAccessTokenCredential("");
}
