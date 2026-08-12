using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Reach.Infra.ConsolePrompt;
using Reach.Infra.Credentials;
using Reach.Infra.JsonConfig;

namespace Reach.Relay.Confluence;

public sealed class ConfluenceRelaySetup : IRelaySetup
{
    internal const string ConfigFileName = "config.json";
    internal const string CredentialFileName = "confluence.credential";
    internal const string CredentialKeyDirectoryName = "credential-keys";
    internal const string CredentialPurpose = "Reach.ConfluenceCredential.v1";

    private readonly IConsolePrompter _prompter;
    private readonly ICredentialFileStore _credentialStore;
    private readonly IConfluenceHttpClientFactory _httpClientFactory;

    public ConfluenceRelaySetup(
        IConsolePrompter prompter,
        ICredentialFileStore credentialStore,
        IConfluenceHttpClientFactory httpClientFactory
    )
    {
        _prompter = prompter;
        _credentialStore = credentialStore;
        _httpClientFactory = httpClientFactory;
    }

    public string Name => "confluence";

    public bool IsComplete(string appDataBasePath)
    {
        try
        {
            if (!File.Exists(Path.Combine(appDataBasePath, CredentialFileName)))
            {
                return false;
            }

            var configFile = new JsonConfigFile(Path.Combine(appDataBasePath, ConfigFileName));

            if (configFile.Read()[ConfluenceOptions.SectionName] is not JsonObject section)
            {
                return false;
            }

            return section[nameof(ConfluenceOptions.LocalToRemotePageId)]?.GetValue<long>() > 0
                && section[nameof(ConfluenceOptions.RemoteToLocalPageId)]?.GetValue<long>() > 0;
        }
        catch (Exception ex)
            when (ex is JsonException or FormatException or InvalidOperationException or IOException
            )
        {
            return false;
        }
    }

    public async Task<int> Run(string basePath, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Confluence URL (set at build time): {BuildConfig.BaseUrl}");
        Console.WriteLine();

        Console.WriteLine(
            "Create two Confluence pages first: one for commands (local -> remote), one for results (remote -> local)."
        );

        var localToRemotePageId = PromptPageId("Commands (local -> remote) page ID");
        var remoteToLocalPageId = PromptPageId("Results (remote -> local) page ID");

        var personalAccessToken = _prompter.PromptSecret("Confluence personal access token");

        Console.WriteLine();
        Console.WriteLine("Validating against Confluence...");

        var validationError = await Validate(
                personalAccessToken,
                localToRemotePageId,
                remoteToLocalPageId,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (validationError is not null)
        {
            Console.WriteLine($"Validation failed: {validationError}");

            return 1;
        }

        Directory.CreateDirectory(basePath);

        _credentialStore.Save(personalAccessToken);

        var configFilePath = Path.Combine(basePath, ConfigFileName);

        var configFile = new JsonConfigFile(configFilePath);

        configFile.SetSection(
            ConfluenceOptions.SectionName,
            new JsonObject
            {
                [nameof(ConfluenceOptions.LocalToRemotePageId)] = localToRemotePageId,
                [nameof(ConfluenceOptions.RemoteToLocalPageId)] = remoteToLocalPageId,
                [nameof(ConfluenceOptions.PollDelayMs)] = 5000,
                [nameof(ConfluenceOptions.Count)] = 100,
            }
        );

        Console.WriteLine("Credential saved (protected for current user)");
        Console.WriteLine($"Configuration saved to {configFilePath}");

        Console.WriteLine("Setup complete.");

        return 0;
    }

    private async Task<string?> Validate(
        string personalAccessToken,
        long localToRemotePageId,
        long remoteToLocalPageId,
        CancellationToken cancellationToken
    )
    {
        using var httpClient = _httpClientFactory.CreateClient(
            new ConfluencePersonalAccessTokenCredential(personalAccessToken)
        );

        httpClient.Timeout = TimeSpan.FromSeconds(5);

        try
        {
            await ConfluenceConfigurationCheck
                .CheckAsync(httpClient, localToRemotePageId, remoteToLocalPageId, cancellationToken)
                .ConfigureAwait(false);

            return null;
        }
        catch (HttpRequestException ex) when (ConfluenceConfigurationCheck.IsConfigurationError(ex))
        {
            return ex.StatusCode == HttpStatusCode.NotFound
                ? "one or more pages were not found. Check the page IDs."
                : $"authentication failed ({(int)ex.StatusCode!}). Check the personal access token.";
        }
        catch (HttpRequestException ex)
        {
            return $"request failed: {ex.Message}";
        }
        catch (TaskCanceledException)
        {
            return "request timed out. Check the Confluence URL and network connectivity.";
        }
    }

    private long PromptPageId(string label)
    {
        while (true)
        {
            if (long.TryParse(_prompter.PromptText(label), out var pageId) && pageId > 0)
            {
                return pageId;
            }

            Console.WriteLine("Enter a positive numeric page ID.");
        }
    }
}
