using Reach.Hosting;
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
        services.AddHosting(configuration, compositionPaths.AppDataBasePath);

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
        return services;
    }
}
