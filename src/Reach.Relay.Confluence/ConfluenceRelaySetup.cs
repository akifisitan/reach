using System.Net;
using System.Security.Cryptography;
using Reach.Cli;
using Reach.Infra.Credentials;

namespace Reach.Relay.Confluence;

public sealed class ConfluenceRelaySetup : IRelaySetup
{
    internal const string ConfigFileName = "confluence.json";
    internal const string CredentialFileName = "confluence.credential";
    internal const string CredentialKeyDirectoryName = "credential-keys";
    internal const string CredentialPurpose = "Reach.ConfluenceCredential.v1";

    private readonly IConsolePrompter _consolePrompter;
    private readonly IConsoleWriter _consoleWriter;
    private readonly ICredentialFileStore _credentialStore;
    private readonly ConfluenceConfigurationStore _configurationStore;
    private readonly IConfluenceHttpClientFactory _httpClientFactory;

    public ConfluenceRelaySetup(
        IConsolePrompter consolePrompter,
        IConsoleWriter consoleWriter,
        ICredentialFileStore credentialStore,
        ConfluenceConfigurationStore configurationStore,
        IConfluenceHttpClientFactory httpClientFactory
    )
    {
        _consolePrompter = consolePrompter;
        _consoleWriter = consoleWriter;
        _credentialStore = credentialStore;
        _configurationStore = configurationStore;
        _httpClientFactory = httpClientFactory;
    }

    public string Name => "confluence";

    public async Task<int> Run(CancellationToken cancellationToken)
    {
        var options = LoadExistingOptions();
        var personalAccessToken = LoadExistingCredential();

        if (options is null)
        {
            return 1;
        }

        _consoleWriter.WriteLine();
        _consoleWriter.WriteLine(
            "Create two Confluence pages: one for commands (local -> remote), one for results (remote -> local)."
        );

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            options = options with
            {
                BaseUrl = PromptBaseUrl(options.BaseUrl),
                LocalToRemotePageId = PromptPageId(
                    "Commands (local -> remote) page ID",
                    options.LocalToRemotePageId
                ),
                RemoteToLocalPageId = PromptDistinctPageId(
                    "Results (remote -> local) page ID",
                    options.RemoteToLocalPageId,
                    options.LocalToRemotePageId
                ),
            };

            personalAccessToken = PromptCredential(personalAccessToken);

            _consoleWriter.WriteLine();
            _consoleWriter.WriteLine("Validating against Confluence...");

            var validationError = await Validate(options, personalAccessToken, cancellationToken)
                .ConfigureAwait(false);

            if (validationError is null)
            {
                break;
            }

            _consoleWriter.WriteLine($"Validation failed: {validationError}");
            _consoleWriter.WriteLine("Review the values and try again.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var json = _configurationStore.Serialize(options);

        _credentialStore.Save(personalAccessToken);
        _configurationStore.SaveAndApply(json, options);

        _consoleWriter.WriteLine("Credential saved (protected for current user)");
        _consoleWriter.WriteLine($"Configuration saved to {_configurationStore.FilePath}");
        _consoleWriter.WriteLine("Setup complete.");

        return 0;
    }

    private async Task<string?> Validate(
        ConfluenceOptions options,
        string personalAccessToken,
        CancellationToken cancellationToken
    )
    {
        using var httpClient = _httpClientFactory.CreateClient(
            new ConfluencePersonalAccessTokenCredential(personalAccessToken),
            options.BaseUrl
        );

        httpClient.Timeout = TimeSpan.FromSeconds(5);

        try
        {
            await ConfluenceConfigurationCheck
                .CheckAsync(
                    httpClient,
                    options.LocalToRemotePageId,
                    options.RemoteToLocalPageId,
                    cancellationToken
                )
                .ConfigureAwait(false);

            return null;
        }
        catch (HttpRequestException ex) when (ex.IsConfigurationError())
        {
            return ex.StatusCode == HttpStatusCode.NotFound
                ? "one or more pages were not found. Check the page IDs."
                : $"authentication failed ({(int)ex.StatusCode!}). Check the personal access token.";
        }
        catch (HttpRequestException ex)
        {
            return $"request failed: {ex.Message}";
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return "request timed out. Check the Confluence URL and network connectivity.";
        }
    }

    private ConfluenceOptions? LoadExistingOptions()
    {
        if (_configurationStore.TryLoad(out var errorMessage))
        {
            return _configurationStore.Current;
        }

        if (!File.Exists(_configurationStore.FilePath))
        {
            return ConfluenceOptions.Empty;
        }

        _consoleWriter.WriteLine(
            $"Confluence configuration at '{_configurationStore.FilePath}' is invalid:{Environment.NewLine}{errorMessage}"
        );

        return IsConfirmed("Overwrite the invalid configuration? Type 'yes' to continue")
            ? ConfluenceOptions.Empty
            : null;
    }

    private string? LoadExistingCredential()
    {
        try
        {
            var credential = _credentialStore.Load();
            return string.IsNullOrWhiteSpace(credential) ? null : credential;
        }
        catch (Exception ex)
            when (ex
                    is CryptographicException
                        or IOException
                        or InvalidOperationException
                        or UnauthorizedAccessException
            )
        {
            return null;
        }
    }

    private string PromptBaseUrl(string currentValue)
    {
        while (true)
        {
            var value = _consolePrompter.PromptText(
                "Confluence server URL",
                string.IsNullOrWhiteSpace(currentValue) ? null : currentValue
            );

            if (ConfluenceOptions.TryNormalizeBaseUrl(value, out var normalized))
            {
                return normalized;
            }

            _consoleWriter.WriteLine(
                "Enter an HTTP or HTTPS origin without a path, query, or fragment."
            );
        }
    }

    private long PromptPageId(string label, long currentValue)
    {
        while (true)
        {
            var defaultValue = currentValue > 0 ? currentValue.ToString() : null;

            if (
                long.TryParse(_consolePrompter.PromptText(label, defaultValue), out var pageId)
                && pageId > 0
            )
            {
                return pageId;
            }

            _consoleWriter.WriteLine("Enter a positive numeric page ID.");
        }
    }

    private long PromptDistinctPageId(string label, long currentValue, long otherPageId)
    {
        while (true)
        {
            var pageId = PromptPageId(label, currentValue);

            if (pageId != otherPageId)
            {
                return pageId;
            }

            _consoleWriter.WriteLine("The two Confluence page IDs must be different.");
            currentValue = 0;
        }
    }

    private string PromptCredential(string? currentValue)
    {
        var hasCurrentValue = !string.IsNullOrWhiteSpace(currentValue);
        var value = _consolePrompter.PromptSecret(
            hasCurrentValue
                ? "Confluence personal access token (press Enter to keep the current token)"
                : "Confluence personal access token",
            allowEmpty: hasCurrentValue
        );

        return string.IsNullOrEmpty(value) ? currentValue! : value;
    }

    private bool IsConfirmed(string label) =>
        string.Equals(
            _consolePrompter.PromptText(label),
            "yes",
            StringComparison.OrdinalIgnoreCase
        );
}
