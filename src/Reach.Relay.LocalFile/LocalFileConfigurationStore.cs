using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Reach.Relay.LocalFile;

public sealed class LocalFileConfigurationStore
{
    private readonly LocalFileOptionsValidator _validator = new();

    public LocalFileConfigurationStore(string basePath)
    {
        BasePath = basePath;
        FilePath = Path.Combine(basePath, LocalFileRelaySetup.ConfigFileName);
    }

    public string BasePath { get; }
    public string FilePath { get; }
    public LocalFileRelayOptions Current
    {
        get =>
            field
            ?? throw new InvalidOperationException(
                $"{nameof(TryLoad)} must be called before accessing {nameof(Current)}"
            );
        private set;
    }

    public bool TryLoad([NotNullWhen(false)] out string? errorMessage)
    {
        if (!File.Exists(FilePath))
        {
            errorMessage = $"Configuration file not found at '{FilePath}'.";
            return false;
        }

        try
        {
            var options = JsonSerializer.Deserialize(
                File.ReadAllText(FilePath),
                LocalFileJsonSerializerContext.Default.LocalFileRelayOptions
            );

            if (options is null)
            {
                errorMessage = "Configuration must contain a JSON object.";
                return false;
            }

            options = Normalize(options);
            var validationResult = _validator.Validate(options);

            if (!validationResult.IsValid)
            {
                errorMessage = string.Join(
                    Environment.NewLine,
                    validationResult.Errors.Select(x => $"{x.PropertyName}: {x.ErrorMessage}")
                );
                return false;
            }

            Current = options;
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
            when (ex
                    is ArgumentException
                        or JsonException
                        or IOException
                        or NotSupportedException
                        or UnauthorizedAccessException
            )
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    public string Serialize(LocalFileRelayOptions options)
    {
        options = Normalize(options);
        var validationResult = _validator.Validate(options);

        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException(
                string.Join(
                    Environment.NewLine,
                    validationResult.Errors.Select(x => $"{x.PropertyName}: {x.ErrorMessage}")
                )
            );
        }

        return JsonSerializer.Serialize(
            options,
            LocalFileJsonSerializerContext.Default.LocalFileRelayOptions
        );
    }

    public void SaveAndApply(string json, LocalFileRelayOptions options)
    {
        Directory.CreateDirectory(BasePath);
        var temporaryFilePath = $"{FilePath}.{Guid.NewGuid():N}.tmp";

        try
        {
            File.WriteAllText(temporaryFilePath, json);
            File.Move(temporaryFilePath, FilePath, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryFilePath);
        }

        Current = Normalize(options);
    }

    private static LocalFileRelayOptions Normalize(LocalFileRelayOptions options) =>
        options with
        {
            LocalToRemoteFilePath = Path.GetFullPath(options.LocalToRemoteFilePath),
            RemoteToLocalFilePath = Path.GetFullPath(options.RemoteToLocalFilePath),
        };
}
