using Reach.Cli;

namespace Reach.Relay.LocalFile;

public sealed class LocalFileRelaySetup : IRelaySetup
{
    internal const string ConfigFileName = "local-file.json";

    private readonly IConsolePrompter _consolePrompter;
    private readonly IConsoleWriter _consoleWriter;
    private readonly LocalFileConfigurationStore _configurationStore;

    public LocalFileRelaySetup(
        IConsolePrompter consolePrompter,
        IConsoleWriter consoleWriter,
        LocalFileConfigurationStore configurationStore
    )
    {
        _consolePrompter = consolePrompter;
        _consoleWriter = consoleWriter;
        _configurationStore = configurationStore;
    }

    public string Name => "local-file";

    public Task<int> Run(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = LoadExistingOptions();

        if (options is null)
        {
            return Task.FromResult(1);
        }

        options = options with
        {
            LocalToRemoteFilePath = PromptPath(
                "Commands (local -> remote) file path",
                options.LocalToRemoteFilePath
            ),
            RemoteToLocalFilePath = PromptDistinctPath(
                "Results (remote -> local) file path",
                options.RemoteToLocalFilePath,
                options.LocalToRemoteFilePath
            ),
        };

        if (HasContent(options.LocalToRemoteFilePath) || HasContent(options.RemoteToLocalFilePath))
        {
            var confirmed = IsConfirmed(
                "Setup will clear both relay files. Type 'yes' to continue"
            );

            if (!confirmed)
            {
                _consoleWriter.WriteLine("Setup cancelled.");
                return Task.FromResult(1);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        CreateEmptyFile(options.LocalToRemoteFilePath);
        CreateEmptyFile(options.RemoteToLocalFilePath);

        var json = _configurationStore.Serialize(options);
        _configurationStore.SaveAndApply(json, options);

        _consoleWriter.WriteLine(
            $"Commands file: {Path.GetFullPath(options.LocalToRemoteFilePath)}"
        );
        _consoleWriter.WriteLine(
            $"Results file: {Path.GetFullPath(options.RemoteToLocalFilePath)}"
        );
        _consoleWriter.WriteLine($"Configuration saved to {_configurationStore.FilePath}");
        _consoleWriter.WriteLine("Setup complete.");

        return Task.FromResult(0);
    }

    private LocalFileRelayOptions? LoadExistingOptions()
    {
        if (_configurationStore.TryLoad(out var errorMessage))
        {
            return _configurationStore.Current;
        }

        if (!File.Exists(_configurationStore.FilePath))
        {
            return LocalFileRelayOptions.CreateDefault(_configurationStore.BasePath);
        }

        _consoleWriter.WriteLine(
            $"Local-file configuration at '{_configurationStore.FilePath}' is invalid:{Environment.NewLine}{errorMessage}"
        );

        return IsConfirmed("Overwrite the invalid configuration? Type 'yes' to continue")
            ? LocalFileRelayOptions.CreateDefault(_configurationStore.BasePath)
            : null;
    }

    private string PromptPath(string label, string currentValue)
    {
        while (true)
        {
            var value = _consolePrompter.PromptText(label, currentValue);

            try
            {
                return Path.GetFullPath(value);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
            {
                _consoleWriter.WriteLine("Enter a valid file path.");
            }
        }
    }

    private string PromptDistinctPath(string label, string currentValue, string otherPath)
    {
        while (true)
        {
            var value = PromptPath(label, currentValue);

            if (!string.Equals(value, otherPath, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }

            _consoleWriter.WriteLine("The two relay file paths must be different.");
        }
    }

    private static bool HasContent(string filePath) =>
        File.Exists(filePath) && new FileInfo(filePath).Length > 0;

    private static void CreateEmptyFile(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, string.Empty);
    }

    private bool IsConfirmed(string label) =>
        string.Equals(
            _consolePrompter.PromptText(label),
            "yes",
            StringComparison.OrdinalIgnoreCase
        );
}
