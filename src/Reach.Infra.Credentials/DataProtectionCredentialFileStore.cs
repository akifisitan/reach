using Microsoft.AspNetCore.DataProtection;

namespace Reach.Infra.Credentials;

public sealed class DataProtectionCredentialFileStore : ICredentialFileStore
{
    private const UnixFileMode OwnerDirectoryMode =
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    private const UnixFileMode OwnerFileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;

    private readonly string _filePath;
    private readonly DirectoryInfo _keyDirectory;
    private readonly IDataProtector _protector;

    public DataProtectionCredentialFileStore(
        string filePath,
        string keyDirectoryPath,
        string purpose
    )
    {
        _filePath = filePath;
        _keyDirectory = new DirectoryInfo(keyDirectoryPath);

        _protector = DataProtectionProvider
            .Create(
                _keyDirectory,
                builder =>
                {
                    builder.SetApplicationName("Reach");

                    if (OperatingSystem.IsWindows())
                    {
                        builder.ProtectKeysWithDpapi();
                    }
                }
            )
            .CreateProtector(purpose);
    }

    public void Save(string secret)
    {
        EnsureDirectory(_keyDirectory.FullName);

        var directory = Path.GetDirectoryName(_filePath);

        if (!string.IsNullOrEmpty(directory))
        {
            EnsureDirectory(directory);
        }

        var temporaryFilePath = $"{_filePath}.{Guid.NewGuid():N}.tmp";

        try
        {
            File.WriteAllText(temporaryFilePath, _protector.Protect(secret));
            RestrictFile(temporaryFilePath);
            File.Move(temporaryFilePath, _filePath, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryFilePath);
        }

        RestrictKeyFiles();
    }

    public string Load()
    {
        if (!File.Exists(_filePath))
        {
            throw new InvalidOperationException($"No credential found at '{_filePath}'.");
        }

        return _protector.Unprotect(File.ReadAllText(_filePath).Trim());
    }

    private static void EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, OwnerDirectoryMode);
        }
    }

    private static void RestrictFile(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, OwnerFileMode);
        }
    }

    private void RestrictKeyFiles()
    {
        if (OperatingSystem.IsWindows() || !_keyDirectory.Exists)
        {
            return;
        }

        foreach (var file in _keyDirectory.EnumerateFiles())
        {
            file.UnixFileMode = OwnerFileMode;
        }
    }
}
