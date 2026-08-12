using Reach.Infra.Credentials;

namespace Reach.Relay.Confluence;

internal sealed class ConfluenceUserCredentialProvider : IConfluenceUserCredentialProvider
{
    private readonly ICredentialFileStore _credentialStore;

    public ConfluenceUserCredentialProvider(ICredentialFileStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    public ConfluenceUserCredential GetCredential() =>
        new ConfluencePersonalAccessTokenCredential(_credentialStore.Load());
}
