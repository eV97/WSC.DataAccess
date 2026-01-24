using System.Data;
using WSC.DataAccess.Mapping;

namespace WSC.DataAccess.Core;

/// <summary>
/// SQL Connection - wraps IDbConnection with SQL map context
/// Allows statement execution using SQL map statement IDs
/// </summary>
public class SqlConnection : ISqlConnection
{
    private readonly IDbConnection _innerConnection;
    private readonly SqlMapConfig _sqlMapConfig;
    private readonly string? _daoName;
    private readonly string _connectionName;

    public SqlConnection(
        IDbConnection innerConnection,
        SqlMapConfig sqlMapConfig,
        string? daoName = null,
        string connectionName = "Default")
    {
        _innerConnection = innerConnection ?? throw new ArgumentNullException(nameof(innerConnection));
        _sqlMapConfig = sqlMapConfig ?? throw new ArgumentNullException(nameof(sqlMapConfig));
        _daoName = daoName;
        _connectionName = connectionName;
    }

    /// <summary>
    /// Gets the SQL map configuration
    /// </summary>
    public SqlMapConfig SqlMapConfig => _sqlMapConfig;

    /// <summary>
    /// Gets the underlying raw connection
    /// </summary>
    public IDbConnection InnerConnection => _innerConnection;

    /// <summary>
    /// Gets the DAO name
    /// </summary>
    public string? DaoName => _daoName;

    /// <summary>
    /// Gets the connection name
    /// </summary>
    public string ConnectionName => _connectionName;

    #region IDbConnection Implementation (Delegate to InnerConnection)

    public string ConnectionString
    {
        get => _innerConnection.ConnectionString;
        set => _innerConnection.ConnectionString = value;
    }

    public int ConnectionTimeout => _innerConnection.ConnectionTimeout;

    public string Database => _innerConnection.Database;

    public ConnectionState State => _innerConnection.State;

    public IDbTransaction BeginTransaction()
        => _innerConnection.BeginTransaction();

    public IDbTransaction BeginTransaction(IsolationLevel il)
        => _innerConnection.BeginTransaction(il);

    public void ChangeDatabase(string databaseName)
        => _innerConnection.ChangeDatabase(databaseName);

    public void Close()
        => _innerConnection.Close();

    public IDbCommand CreateCommand()
        => _innerConnection.CreateCommand();

    public void Open()
        => _innerConnection.Open();

    public void Dispose()
        => _innerConnection.Dispose();

    #endregion
}
