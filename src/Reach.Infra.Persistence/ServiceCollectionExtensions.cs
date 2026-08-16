using Reach.Options;
using Reach.Relay;

namespace Reach.Infra.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddSingleton<DbConnectionFactory>();
        services.AddSingleton<DatabaseInitializer>();
        services.AddSingleton<IRelayCursorStorage>(sp =>
            sp.GetRequiredService<RelayCursorRepository>()
        );
        services.AddSingleton<IDbInitializable>(sp =>
            sp.GetRequiredService<RelayCursorRepository>()
        );
        services.AddSingleton<RelayCursorRepository>();
        services
            .AddOptions<RelayCursorRepositoryOptions>()
            .Configure(x =>
            {
                x.TableName = "relay_cursor_settings";
            })
            .WithValidator<RelayCursorRepositoryOptions, RelayCursorRepositoryOptionsValidator>();
        services
            .AddOptions<DbConnectionFactoryOptions>()
            .WithValidator<DbConnectionFactoryOptions, DbConnectionFactoryOptionsValidator>();

        return services;
    }
}
