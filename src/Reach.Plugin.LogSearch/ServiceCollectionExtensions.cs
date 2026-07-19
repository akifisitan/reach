using Microsoft.Extensions.DependencyInjection;
using Reach.Plugin.Abstractions;
using Reach.Plugin.LogSearch.LogSearchStart;
using Reach.Plugin.LogSearch.LogSearchStatus;

namespace Reach.Plugin.LogSearch;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLogSearchPlugin(this IServiceCollection services)
    {
        services.AddSingleton<FileSearcher>();
        services.AddSingleton<SearchResultStore>();

        // 1 or more ICliCommand
        services.AddSingleton<ICliCommand, SearchCliCommand>();

        // 1 or more IJsonTypeInfoRegistration
        // 1 or more Handler registration
        // Handler is either 2
        // IHandler<CliCommandPayload, CliCommandResult>
        // IHandler<RpcPayload, RpcResult>
        // or 4
        // IHandler<CliCommandPayload, CliCommandResult>
        // IHandler<RpcPayload, RpcResult>
        // IHandler<RelayCommand, RelayCommandResult>
        // IHandler<RelayCommandResult, Unit>

        // LogSearchStart
        services.AddSingleton<SearchStartCliCommand>();
        services.AddSingleton<IJsonTypeInfoRegistration, LogSearchStartJsonRegistration>();
        services.AddHandlers<
            SearchStartCliCommandHandler,
            LogSearchStartHandler,
            SearchFileCommandHandler,
            SearchFileCommandResultHandler
        >();

        // LogSearchStatus
        services.AddSingleton<SearchStatusCliCommand>();
        services.AddSingleton<IJsonTypeInfoRegistration, LogSearchStatusJsonRegistration>();
        services.AddHandlers<SearchStatusCliCommandHandler, LogSearchStatusHandler>();

        return services;
    }
}
