using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Reach.Relay.Confluence;

public sealed class ConfluenceConfigurationStore
{
    private readonly ConfluenceOptionsValidator _validator = new();

    public ConfluenceConfigurationStore(string basePath)
    {
        FilePath = Path.Combine(basePath, ConfluenceRelaySetup.ConfigFileName);
    }

    public string FilePath { get; }
    public ConfluenceOptions Current
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
                ConfluenceJsonSerializerContext.Default.ConfluenceOptions
            );

            if (options is null)
            {
                errorMessage = "Configuration must contain a JSON object.";
                return false;
            }

            var validationResult = _validator.Validate(options);

            if (!validationResult.IsValid)
            {
                errorMessage = string.Join(
                    Environment.NewLine,
                    validationResult.Errors.Select(x => $"{x.PropertyName}: {x.ErrorMessage}")
                );
                return false;
            }

            options = options with
            {
                BaseUrl = ConfluenceOptions.NormalizeBaseUrl(options.BaseUrl),
            };

            Current = options;
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
            when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    public string Serialize(ConfluenceOptions options)
    {
        options = options with { BaseUrl = ConfluenceOptions.NormalizeBaseUrl(options.BaseUrl) };

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
            ConfluenceJsonSerializerContext.Default.ConfluenceOptions
        );
    }

    public void SaveAndApply(string json, ConfluenceOptions options)
    {
        var directory = Path.GetDirectoryName(FilePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

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

        Current = options;
    }
}
