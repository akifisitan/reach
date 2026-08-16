using Reach.Infra.ConsolePrompt;
using Reach.Infra.Credentials;
using Reach.Options;

namespace Reach.Relay.Confluence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConfluenceRelay(
        this IServiceCollection services,
        IConfiguration configuration,
        string basePath
    )
    {
        services.AddSingleton<ConfluenceCommentRelay>();
        services.AddSingleton<ILocalRelay>(sp => sp.GetRequiredService<ConfluenceCommentRelay>());
        services.AddSingleton<IRemoteRelay>(sp => sp.GetRequiredService<ConfluenceCommentRelay>());
        services.AddSingleton<IConsolePrompter, ConsolePrompter>();
        services.AddSingleton<IConfluenceHttpClientFactory, ConfluenceHttpClientFactory>();

        services.AddSingleton<ICredentialFileStore>(
            new DataProtectionCredentialFileStore(
                Path.Combine(basePath, ConfluenceRelaySetup.CredentialFileName),
                Path.Combine(basePath, ConfluenceRelaySetup.CredentialKeyDirectoryName),
                ConfluenceRelaySetup.CredentialPurpose
            )
        );
        services.AddSingleton<
            IConfluenceUserCredentialProvider,
            ConfluenceUserCredentialProvider
        >();

        services.AddSingleton<IRelaySetup, ConfluenceRelaySetup>();

        services.AddSingleton<IStartupCheck, ConfluenceStartupCheck>();

        services
            .AddOptions<ConfluenceOptions>()
            .Bind(configuration.GetSection(ConfluenceOptions.SectionName))
            .WithValidator<ConfluenceOptions, ConfluenceOptionsValidator>();

        return services;
    }
}
