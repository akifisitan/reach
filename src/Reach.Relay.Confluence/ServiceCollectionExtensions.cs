using Reach.Infra.Credentials;

namespace Reach.Relay.Confluence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConfluenceRelay(
        this IServiceCollection services,
        string basePath
    )
    {
        if (services.Any(x => x.ServiceType == typeof(IRelayRegistration)))
        {
            throw new InvalidOperationException("A relay has already been registered.");
        }

        services.AddSingleton<IRelayRegistration>(new RelayRegistration("confluence"));
        services.AddSingleton(new ConfluenceConfigurationStore(basePath));
        services.AddSingleton<ConfluenceCommentRelay>();
        services.AddSingleton<ILocalRelay>(sp => sp.GetRequiredService<ConfluenceCommentRelay>());
        services.AddSingleton<IRemoteRelay>(sp => sp.GetRequiredService<ConfluenceCommentRelay>());
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

        return services;
    }
}
