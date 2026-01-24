using System.Data;

namespace WSC.DataAccess.Core;

/// <summary>
/// SQL Service interface - iBatis.NET style API
/// Provides direct connection access with DAO switching
/// </summary>
public interface ISql
{
    /// <summary>
    /// Creates a new database connection wrapped with SQL map capabilities
    /// Connection is ready to execute SQL map statements
    /// </summary>
    /// <returns>Database connection with statement execution extensions</returns>
    ISqlMapConnection CreateConnection();

    /// <summary>
    /// Creates a new database connection for a specific named connection
    /// </summary>
    /// <param name="connectionName">Named connection (e.g., "MainDB", "ReportDB")</param>
    /// <returns>Database connection with statement execution extensions</returns>
    ISqlMapConnection CreateConnection(string connectionName);

    /// <summary>
    /// Switches the current DAO context
    /// Subsequent statement executions will use this DAO's SQL map file
    /// </summary>
    /// <param name="daoName">DAO name (e.g., "DAO000", "DAO001")</param>
    void GetDAO(string daoName);

    /// <summary>
    /// Switches the current DAO context for a specific connection
    /// </summary>
    /// <param name="daoName">DAO name</param>
    /// <param name="connectionName">Named connection</param>
    void GetDAO(string daoName, string connectionName);

    /// <summary>
    /// Gets the currently active DAO name
    /// </summary>
    string? CurrentDao { get; }

    /// <summary>
    /// Gets the currently active connection name
    /// </summary>
    string CurrentConnection { get; }
}
