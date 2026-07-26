using Reach.Infra.Credentials;

namespace Reach.Relay.Confluence;

internal sealed class DpapiConfluenceUserCredentialProvider : IConfluenceUserCredentialProvider
{
    private readonly ICredentialFileStore _credentialStore;

    public DpapiConfluenceUserCredentialProvider(ICredentialFileStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    public ConfluenceUserCredential GetCredential() =>
        new ConfluencePersonalAccessTokenCredential(_credentialStore.Load());
}
