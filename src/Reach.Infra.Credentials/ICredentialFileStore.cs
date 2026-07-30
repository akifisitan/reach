namespace Reach.Infra.Credentials;

public interface ICredentialFileStore
{
    void Save(string secret);
    string Load();
}
