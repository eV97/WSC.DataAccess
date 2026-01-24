using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Mapping;

namespace WSC.DataAccess.Core;

/// <summary>
/// SQL Service - iBatis.NET style API implementation
/// Provides direct connection access with DAO switching
/// </summary>
public class SqlService : ISql
{
    private readonly IDbSessionFactory _sessionFactory;
    private readonly SqlMapProvider _sqlMapProvider;
    private readonly ILogger<SqlService>? _logger;

    // Thread-safe storage for current DAO context
    private static readonly AsyncLocal<string?> _currentDao = new();
    private static readonly AsyncLocal<string> _currentConnection = new();

    public SqlService(
        IDbSessionFactory sessionFactory,
        SqlMapProvider sqlMapProvider,
        ILogger<SqlService>? logger = null)
    {
        _sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        _sqlMapProvider = sqlMapProvider ?? throw new ArgumentNullException(nameof(sqlMapProvider));
        _logger = logger;

        // Set default connection
        _currentConnection.Value = SqlMapProvider.DEFAULT_CONNECTION;
    }

    /// <summary>
    /// Gets the currently active DAO name
    /// </summary>
    public string? CurrentDao => _currentDao.Value;

    /// <summary>
    /// Gets the currently active connection name
    /// </summary>
    public string CurrentConnection => _currentConnection.Value ?? SqlMapProvider.DEFAULT_CONNECTION;

    /// <summary>
    /// Switches the current DAO context (for default connection)
    /// </summary>
    public void GetDAO(string daoName)
    {
        GetDAO(daoName, CurrentConnection);
    }

    /// <summary>
    /// Switches the current DAO context for a specific connection
    /// </summary>
    public void GetDAO(string daoName, string connectionName)
    {
        if (string.IsNullOrWhiteSpace(daoName))
            throw new ArgumentException("DAO name cannot be null or empty", nameof(daoName));

        if (string.IsNullOrWhiteSpace(connectionName))
            throw new ArgumentException("Connection name cannot be null or empty", nameof(connectionName));

        // Validate DAO is registered
        if (!_sqlMapProvider.HasFile(daoName, connectionName))
        {
            throw new InvalidOperationException(
                $"DAO '{daoName}' not found in provider for connection '{connectionName}'. " +
                $"Please register it in ConfigureSqlMaps(). " +
                $"Example: provider.AddFile(\"{daoName}\", \"SqlMaps/YourFile.xml\", \"{connectionName}\")");
        }

        _currentDao.Value = daoName;
        _currentConnection.Value = connectionName;

        _logger?.LogDebug("Switched DAO context to '{DaoName}' on connection '{ConnectionName}'",
            daoName, connectionName);
    }

    /// <summary>
    /// Creates a new database connection with SQL map capabilities (default connection)
    /// </summary>
    public ISqlMapConnection CreateConnection()
    {
        return CreateConnection(CurrentConnection);
    }

    /// <summary>
    /// Creates a new database connection for a specific named connection
    /// </summary>
    public ISqlMapConnection CreateConnection(string connectionName)
    {
        if (string.IsNullOrWhiteSpace(connectionName))
            connectionName = SqlMapProvider.DEFAULT_CONNECTION;

        var currentDao = CurrentDao;

        if (string.IsNullOrWhiteSpace(currentDao))
        {
            throw new InvalidOperationException(
                "No DAO context set. Call GetDAO(daoName) before creating a connection. " +
                "Example: _sql.GetDAO(Provider.DAO000);");
        }

        // Get file path from provider
        var filePath = _sqlMapProvider.GetFilePath(currentDao, connectionName);
        if (string.IsNullOrEmpty(filePath))
        {
            throw new InvalidOperationException(
                $"SQL map file not found for DAO '{currentDao}' on connection '{connectionName}'");
        }

        _logger?.LogDebug("Creating connection for DAO '{DaoName}' on connection '{ConnectionName}', file: {FilePath}",
            currentDao, connectionName, filePath);

        // Load SQL map config for this DAO
        var sqlMapConfig = SqlMapConfigBuilder.FromFile(filePath);

        // Create raw connection
        var session = _sessionFactory.OpenSession(connectionName);
        var rawConnection = session.Connection;

        // Wrap with SQL map context
        var sqlConnection = new SqlMapConnection(
            rawConnection,
            sqlMapConfig,
            currentDao,
            connectionName);

        _logger?.LogInformation("SQL connection created - DAO: {DaoName}, Connection: {ConnectionName}",
            currentDao, connectionName);

        return sqlConnection;
    }
}
