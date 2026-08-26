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
        // TODO: should we document this instead of eating the null check penalty each time or is it negligible?
        get =>
            field
            ?? throw new InvalidOperationException(
                $"{nameof(TryLoad)} must be called before accessing {nameof(Current)}"
            );
        private set;
    }

    public bool TryLoad(out string error)
    {
        if (!File.Exists(FilePath))
        {
            error = $"Configuration file not found at '{FilePath}'.";
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
                error = "Configuration must contain a JSON object.";
                return false;
            }

            var validationResult = _validator.Validate(options);

            if (!validationResult.IsValid)
            {
                error = string.Join(
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
            error = string.Empty;
            return true;
        }
        catch (Exception ex)
            when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            error = ex.Message;
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
