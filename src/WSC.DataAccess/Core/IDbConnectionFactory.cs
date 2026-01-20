using System.Data;

namespace WSC.DataAccess.Core;

/// <summary>
/// Interface for database connection factory
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Creates a new database connection
    /// </summary>
    /// <returns>Database connection instance</returns>
    IDbConnection CreateConnection();

    /// <summary>
    /// Creates a new database connection with specific connection string
    /// </summary>
    /// <param name="connectionString">Connection string to use</param>
    /// <returns>Database connection instance</returns>
    IDbConnection CreateConnection(string connectionString);

    /// <summary>
    /// Gets the default connection string
    /// </summary>
    string ConnectionString { get; }
}
