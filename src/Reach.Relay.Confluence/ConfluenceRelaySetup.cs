using System.Net;
using System.Net.Http.Headers;
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
    internal const string CredentialEntropy = "Reach.ConfluenceCredential.v1";

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

    public async Task<int> RunAsync(string basePath, CancellationToken cancellationToken)
    {
        var prompter = new ConsolePrompter();

        Console.WriteLine($"Confluence URL (set at build time): {BuildConfig.BaseUrl}");
        Console.WriteLine();

        Console.WriteLine(
            "Create two Confluence pages first: one for commands (local -> remote), one for results (remote -> local)."
        );

        var localToRemotePageId = PromptPageId(prompter, "Commands (local -> remote) page ID");
        var remoteToLocalPageId = PromptPageId(prompter, "Results (remote -> local) page ID");

        var personalAccessToken = prompter.PromptSecret("Confluence personal access token");

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

        var credentialFilePath = Path.Combine(basePath, CredentialFileName);

        var credentialStore = new DpapiCredentialFileStore(credentialFilePath, CredentialEntropy);

        credentialStore.Save(personalAccessToken);

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

        Console.WriteLine($"Credential saved to {credentialFilePath} (protected for current user)");
        Console.WriteLine($"Configuration saved to {configFilePath}");

        Console.WriteLine("Setup complete.");

        return 0;
    }

    private static async Task<string?> Validate(
        string personalAccessToken,
        long localToRemotePageId,
        long remoteToLocalPageId,
        CancellationToken cancellationToken
    )
    {
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(BuildConfig.BaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(5),
        };

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            personalAccessToken
        );

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

    private static long PromptPageId(IConsolePrompter prompter, string label)
    {
        while (true)
        {
            if (long.TryParse(prompter.PromptText(label), out var pageId) && pageId > 0)
            {
                return pageId;
            }

            Console.WriteLine("Enter a positive numeric page ID.");
        }
    }
}
