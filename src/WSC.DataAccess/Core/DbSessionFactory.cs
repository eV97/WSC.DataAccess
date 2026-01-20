using Microsoft.Extensions.Logging;

namespace WSC.DataAccess.Core;

/// <summary>
/// Default implementation of database session factory
/// </summary>
public class DbSessionFactory : IDbSessionFactory
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly Dictionary<string, string> _connectionStrings;
    private readonly ILogger<DbSession>? _logger;

    public DbSessionFactory(IDbConnectionFactory connectionFactory, ILogger<DbSession>? logger = null)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _connectionStrings = new Dictionary<string, string>();
        _logger = logger;
    }

    public DbSessionFactory(IDbConnectionFactory connectionFactory, Dictionary<string, string> connectionStrings, ILogger<DbSession>? logger = null)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _connectionStrings = connectionStrings ?? new Dictionary<string, string>();
        _logger = logger;
    }

    /// <inheritdoc/>
    public DbSession OpenSession()
    {
        var connection = _connectionFactory.CreateConnection();
        return new DbSession(connection, _logger);
    }

    /// <inheritdoc/>
    public DbSession OpenSession(string connectionName)
    {
        if (!_connectionStrings.TryGetValue(connectionName, out var connectionString))
        {
            throw new ArgumentException($"Connection string '{connectionName}' not found", nameof(connectionName));
        }

        var connection = _connectionFactory.CreateConnection(connectionString);
        return new DbSession(connection, _logger);
    }

    /// <summary>
    /// Adds a named connection string
    /// </summary>
    public void AddConnectionString(string name, string connectionString)
    {
        _connectionStrings[name] = connectionString;
    }
}
