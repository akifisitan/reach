using System.Text.Json;
using Reach.Infra.NativeConsole;
using Reach.Infra.Persistence;
using Reach.Module;
using Reach.Module.Example;
using Reach.Module.LogSearch;
using Reach.Relay;
using Reach.Relay.Confluence;

namespace Reach.Hosting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHosting(
        this IServiceCollection services,
        IConfiguration configuration,
        string basePath
    )
    {
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

        services.AddLogSearchModule();
        services.AddExampleModule();

        return services;
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions(IServiceProvider sp) =>
        WireJsonExtensions.CreateJsonSerializerOptions(
            sp.GetServices<IWireJsonRegistration>().ToList()
        );
}
