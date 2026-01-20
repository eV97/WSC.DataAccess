namespace WSC.DataAccess.Core;

/// <summary>
/// Default implementation of database session factory
/// </summary>
public class DbSessionFactory : IDbSessionFactory
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly Dictionary<string, string> _connectionStrings;

    public DbSessionFactory(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _connectionStrings = new Dictionary<string, string>();
    }

    public DbSessionFactory(IDbConnectionFactory connectionFactory, Dictionary<string, string> connectionStrings)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _connectionStrings = connectionStrings ?? new Dictionary<string, string>();
    }

    /// <inheritdoc/>
    public DbSession OpenSession()
    {
        var connection = _connectionFactory.CreateConnection();
        return new DbSession(connection);
    }

    /// <inheritdoc/>
    public DbSession OpenSession(string connectionName)
    {
        if (!_connectionStrings.TryGetValue(connectionName, out var connectionString))
        {
            throw new ArgumentException($"Connection string '{connectionName}' not found", nameof(connectionName));
        }

        var connection = _connectionFactory.CreateConnection(connectionString);
        return new DbSession(connection);
    }

    /// <summary>
    /// Adds a named connection string
    /// </summary>
    public void AddConnectionString(string name, string connectionString)
    {
        _connectionStrings[name] = connectionString;
    }
}
