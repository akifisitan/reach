using Reach.Plugin.Abstractions;
using Reach.Plugin.LogSearch;
using Reach.Relay.Abstractions;
using Reach.Relay.Confluence;
using Reach.Relay.LocalFile;

namespace Reach.Shared;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddShared(this IServiceCollection services)
    {
        services.AddPluginAbstractions();

        services.AddSingleton<IRelayPayloadSerializer>(CreateSerializer);
        services.AddSingleton<IRpcPayloadSerializer>(CreateSerializer);
        services.AddSingleton<ISingleAppInstanceLock, SingleAppInstanceLock>();

        services.AddLocalFileRelay();

        services
            .AddOptions<LocalFileRelayOptions>()
            .Configure(x =>
            {
                var basePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Reach.Debug",
                    "local",
                    "localrelay"
                );

                Directory.CreateDirectory(basePath);

                x.LocalToRemoteFilePath = Path.Combine(basePath, "relay_local_to_remote.jsonl");
                x.RemoteToLocalFilePath = Path.Combine(basePath, "relay_remote_to_local.jsonl");
            });

        //services
        //    .AddOptions<ConfluenceOptions>()
        //    .Configure(x =>
        //    {
        //        x.BaseUrl = BuildConfig.BaseUrl;
        //        x.LocalToRemotePageId = 801540584;
        //        x.RemoteToLocalPageId = 801540586;
        //        x.PollDelayMs = 5000;
        //        x.Count = 10;
        //    });

        //services.AddConfluenceRelay();

        services.AddSingleton<
            IConfluenceUserCredentialProvider,
            ConfluenceUserCredentialProvider
        >();

        services.AddLogSearchPlugin();

        return services;
    }

    private static RelayJsonPayloadSerializer CreateSerializer(IServiceProvider sp) =>
        new(
            PluginJsonExtensions.CreateJsonSerializerOptions(
                sp.GetServices<IJsonTypeInfoRegistration>().ToList()
            )
        );
}
