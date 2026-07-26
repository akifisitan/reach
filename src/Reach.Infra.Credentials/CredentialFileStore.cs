using System.Security.Cryptography;
using System.Text;

namespace Reach.Infra.Credentials;

public interface ICredentialFileStore
{
    void Save(string secret);
    string Load();
}

public sealed class DpapiCredentialFileStore : ICredentialFileStore
{
    private readonly string _filePath;
    private readonly byte[] _entropy;

    public DpapiCredentialFileStore(string filePath, string entropy)
    {
        _filePath = filePath;
        _entropy = Encoding.UTF8.GetBytes(entropy);
    }

    public void Save(string secret)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "DPAPI credential protection is only supported on Windows."
            );
        }

        var protectedBytes = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(secret),
            _entropy,
            DataProtectionScope.CurrentUser
        );

        var directory = Path.GetDirectoryName(_filePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_filePath, Convert.ToBase64String(protectedBytes));
    }

    public string Load()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "DPAPI credential protection is only supported on Windows."
            );
        }

        if (!File.Exists(_filePath))
        {
            throw new InvalidOperationException($"No credential found at '{_filePath}'.");
        }

        var protectedBytes = Convert.FromBase64String(File.ReadAllText(_filePath).Trim());

        var secretBytes = ProtectedData.Unprotect(
            protectedBytes,
            _entropy,
            DataProtectionScope.CurrentUser
        );

        return Encoding.UTF8.GetString(secretBytes);
    }
}
