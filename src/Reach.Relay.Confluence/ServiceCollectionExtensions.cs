using Reach.Infra.ConsolePrompt;
using Reach.Infra.Credentials;

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
            new DpapiCredentialFileStore(
                Path.Combine(basePath, ConfluenceRelaySetup.CredentialFileName),
                ConfluenceRelaySetup.CredentialEntropy
            )
        );
        services.AddSingleton<
            IConfluenceUserCredentialProvider,
            DpapiConfluenceUserCredentialProvider
        >();

        services.AddSingleton<IRelaySetup, ConfluenceRelaySetup>();

        services.AddSingleton<IStartupCheck, ConfluenceStartupCheck>();

        services
            .AddOptions<ConfluenceOptions>()
            .Bind(configuration.GetSection(ConfluenceOptions.SectionName))
            .PostConfigure(x =>
            {
                x.BaseUrl = BuildConfig.BaseUrl.TrimEnd('/') + "/";
            })
            .Validate(
                x => new ConfluenceOptionsValidator().Validate(x).IsValid,
                "Invalid Confluence configuration. Restart the application to reconfigure."
            )
            .ValidateOnStart();

        return services;
    }
}
