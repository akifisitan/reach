using System.Text.Json;
using Reach.Infra.NativeConsole;
using Reach.Infra.Persistence;
using Reach.Plugin;
using Reach.Plugin.Example;
using Reach.Plugin.LogSearch;
using Reach.Relay;
using Reach.Relay.Confluence;

namespace Reach.Composition;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddComposition(
        this IServiceCollection services,
        IConfiguration configuration,
        string basePath
    )
    {
        services.AddPluginAbstractions();

        services.AddSingleton(CreateJsonSerializerOptions);
        services.AddSingleton(sp => new RelayJsonPayloadSerializer(
            sp.GetRequiredService<JsonSerializerOptions>()
        ));
        services.AddSingleton<IRelayPayloadSerializer>(sp =>
            sp.GetRequiredService<RelayJsonPayloadSerializer>()
        );
        services.AddSingleton<IRpcPayloadSerializer>(sp =>
            sp.GetRequiredService<RelayJsonPayloadSerializer>()
        );
        services.AddSingleton<ISingleAppInstanceLock, SingleAppInstanceLock>();
        services.AddSingleton<IConsoleAttacher, ConsoleAttacher>();
        services.AddSingleton<IRelaySetupCoordinator>(sp => new RelaySetupCoordinator(
            basePath,
            sp.GetRequiredService<IConsoleAttacher>(),
            sp.GetServices<IRelaySetup>()
        ));

        services.AddConfluenceRelay(configuration, basePath);
        //services.AddLocalFileRelay();

        services.AddPersistence();

        services.AddLogSearchPlugin();
        services.AddExamplePlugin();

        return services;
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions(IServiceProvider sp) =>
        PluginJsonExtensions.CreateJsonSerializerOptions(
            sp.GetServices<IJsonTypeInfoRegistration>().ToList()
        );
}
