using Reach.Relay.Confluence;

namespace Reach.Composition;

internal sealed class ConfluenceUserCredentialProvider : IConfluenceUserCredentialProvider
{
    public ConfluenceUserCredential GetCredential() =>
        new ConfluencePersonalAccessTokenCredential("");
}
