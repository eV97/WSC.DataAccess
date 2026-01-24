using System.Data;
using WSC.DataAccess.Mapping;

namespace WSC.DataAccess.Core;

/// <summary>
/// SQL Map Connection interface - wraps IDbConnection with SQL map context
/// Provides statement execution methods with SQL map support
/// </summary>
public interface ISqlMapConnection : IDbConnection
{
    /// <summary>
    /// Gets the SQL map configuration for this connection
    /// </summary>
    SqlMapConfig SqlMapConfig { get; }

    /// <summary>
    /// Gets the underlying raw database connection
    /// </summary>
    IDbConnection InnerConnection { get; }

    /// <summary>
    /// Gets the DAO name associated with this connection
    /// </summary>
    string? DaoName { get; }

    /// <summary>
    /// Gets the connection name (e.g., "MainDB", "ReportDB")
    /// </summary>
    string ConnectionName { get; }
}
