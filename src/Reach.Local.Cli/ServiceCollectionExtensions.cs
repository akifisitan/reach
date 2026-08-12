using Reach.Hosting;
using Reach.Local.Shared;
using Reach.Module;

namespace Reach.Local.Cli;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration,
        ReachAppPaths paths,
        ReachAppPaths compositionPaths
    )
    {
        services.AddLocalShared(configuration, compositionPaths.AppDataBasePath);

        services.AddReachLogging(configuration, paths);

        services.AddAppOptions(configuration);

        services.AddSingleton<IRpcClient, LocalPipeRpcClient>();
        services.AddSingleton<LocalCliExecutor>();
        services.AddSingleton<RootCliCommand>();
        services.AddSingleton<IConsoleWriter, ConsoleWriter>();

        return services;
    }

    private static IServiceCollection AddAppOptions(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<AppOptions>()
            .Bind(configuration.GetRequiredSection(nameof(AppOptions)));

        services
            .AddOptions<LocalPipeRpcClientOptions>()
            .Configure(x =>
            {
                x.ShouldNotBootServer =
                    bool.TryParse(
                        Environment.GetEnvironmentVariable(AppConstants.ShouldNotBootServerEnvVar),
                        out var shouldNotBootServer
                    ) && shouldNotBootServer;
                x.ServerBinaryPath = Path.Combine(Environment.CurrentDirectory, "reachld.exe");
            });

        return services;
    }
}
