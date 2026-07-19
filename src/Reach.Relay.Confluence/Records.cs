namespace Reach.Relay.Confluence;

public sealed record ConfluenceCommentEntry(string Content)
{
    public DateTimeOffset Timestamp { get; } = DateTime.Now;
}

public sealed record ConfluenceBasicAuthCredential(string Username, string Password)
    : ConfluenceUserCredential;

public sealed record ConfluencePersonalAccessTokenCredential(string AccessToken)
    : ConfluenceUserCredential;

public abstract record ConfluenceUserCredential();

public interface IConfluenceUserCredentialProvider
{
    ConfluenceUserCredential GetCredential();
}
