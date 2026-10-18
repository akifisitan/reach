using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reach.Module.SoapSearch.Extraction;
using Reach.Module.SoapSearch.Features.SoapSearchStart;
using Reach.Module.SoapSearch.Features.SoapSearchStatus;
using Reach.Module.Toolkit.ResultStore.Sqlite;

namespace Reach.Module.SoapSearch;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSoapSearchModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string basePath
    )
    {
        services
            .AddOptions<SoapSearchOptions>()
            .Bind(configuration.GetSection(SoapSearchOptions.SectionName))
            .PostConfigure(options =>
                options.MetadataFilePath = Path.GetFullPath(options.MetadataFilePath, basePath)
            );
        services.AddSingleton<ISoapEnvelopeFormatter, SoapEnvelopeFormatter>();
        services.AddSingleton<SoapLogExtractor>();
        services.AddSingleton<SoapSearcher>();
        services.AddSqliteResultStore<SoapSearchCommandResult>(
            "soap_search_results",
            TimeSpan.FromHours(24)
        );
        services.AddRootCliCommand<SoapSearchCliCommand>();
        services.AddSoapSearchStartFeature();
        services.AddSoapSearchStatusFeature();
        return services;
    }
}
