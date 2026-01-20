using Microsoft.Data.SqlClient;
using System.Data;

namespace WSC.DataAccess.Core;

/// <summary>
/// SQL Server connection factory implementation
/// </summary>
public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        _connectionString = connectionString;
    }

    /// <inheritdoc/>
    public string ConnectionString => _connectionString;

    /// <inheritdoc/>
    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    /// <inheritdoc/>
    public IDbConnection CreateConnection(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        return new SqlConnection(connectionString);
    }
}
