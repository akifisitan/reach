using Dapper;

namespace Reach.Infra.Persistence;

public sealed class DatabaseInitializer
{
    private readonly string _dbPath;
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly IReadOnlyList<IDbInitializable> _repositories;

    public DatabaseInitializer(
        DbConnectionFactory dbConnectionFactory,
        ILogger<DatabaseInitializer> logger,
        IOptions<DbConnectionFactoryOptions> options,
        IEnumerable<IDbInitializable> repositories
    )
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
        _dbPath = options.Value.DbPath;
        _repositories = repositories.ToList();
    }

    public void Initialize()
    {
        _logger.ZLogDebug($"Initializing database");

        SQLitePCL.Batteries.Init();

        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);

        using (var connection = _dbConnectionFactory.CreateConnection())
        {
            connection.Execute(
                """
                PRAGMA journal_mode=WAL;
                """
            );
        }

        foreach (var repository in _repositories)
        {
            repository.EnsureCreated();
        }
    }
}
