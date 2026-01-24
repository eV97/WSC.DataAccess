using Dapper;
using WSC.DataAccess.Core;
using WSC.DataAccess.Mapping;

namespace WSC.DataAccess.Extensions;

/// <summary>
/// Extension methods for ISqlMapConnection to execute SQL map statements
/// Provides iBatis.NET style API: connection.StatementExecuteQueryAsync("GetAllAssets")
/// </summary>
public static class SqlConnectionExtensions
{
    /// <summary>
    /// Executes a query statement and returns a list of results
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="connection">SQL connection</param>
    /// <param name="statementId">Statement ID from SQL map (e.g., "GetAllAssets")</param>
    /// <param name="parameters">Query parameters (anonymous object or dictionary)</param>
    /// <returns>List of results</returns>
    public static async Task<IEnumerable<T>> StatementExecuteQueryAsync<T>(
        this ISqlMapConnection connection,
        string statementId,
        object? parameters = null)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));

        if (string.IsNullOrWhiteSpace(statementId))
            throw new ArgumentException("Statement ID cannot be null or empty", nameof(statementId));

        // Get SQL from statement
        var statement = connection.SqlMapConfig.GetStatement(statementId);
        if (statement == null)
        {
            throw new InvalidOperationException(
                $"Statement '{statementId}' not found in SQL map for DAO '{connection.DaoName}'. " +
                $"Check your SQL map file.");
        }

        // Execute using Dapper (Dapper handles parameter parsing)
        return await connection.InnerConnection.QueryAsync<T>(statement.CommandText, parameters);
    }

    /// <summary>
    /// Executes a query statement and returns a single result
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="connection">SQL connection</param>
    /// <param name="statementId">Statement ID from SQL map</param>
    /// <param name="parameters">Query parameters</param>
    /// <returns>Single result or default(T)</returns>
    public static async Task<T?> StatementExecuteSingleAsync<T>(
        this ISqlMapConnection connection,
        string statementId,
        object? parameters = null)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));

        if (string.IsNullOrWhiteSpace(statementId))
            throw new ArgumentException("Statement ID cannot be null or empty", nameof(statementId));

        var statement = connection.SqlMapConfig.GetStatement(statementId);
        if (statement == null)
        {
            throw new InvalidOperationException(
                $"Statement '{statementId}' not found in SQL map for DAO '{connection.DaoName}'");
        }

        return await connection.InnerConnection.QuerySingleOrDefaultAsync<T>(statement.CommandText, parameters);
    }

    /// <summary>
    /// Executes a scalar statement and returns a single value
    /// </summary>
    /// <typeparam name="T">Result type (int, decimal, string, etc.)</typeparam>
    /// <param name="connection">SQL connection</param>
    /// <param name="statementId">Statement ID from SQL map</param>
    /// <param name="parameters">Query parameters</param>
    /// <returns>Scalar value</returns>
    public static async Task<T> StatementExecuteScalarAsync<T>(
        this ISqlMapConnection connection,
        string statementId,
        object? parameters = null)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));

        if (string.IsNullOrWhiteSpace(statementId))
            throw new ArgumentException("Statement ID cannot be null or empty", nameof(statementId));

        var statement = connection.SqlMapConfig.GetStatement(statementId);
        if (statement == null)
        {
            throw new InvalidOperationException(
                $"Statement '{statementId}' not found in SQL map for DAO '{connection.DaoName}'");
        }

        return await connection.InnerConnection.ExecuteScalarAsync<T>(statement.CommandText, parameters);
    }

    /// <summary>
    /// Executes a non-query statement (INSERT, UPDATE, DELETE)
    /// </summary>
    /// <param name="connection">SQL connection</param>
    /// <param name="statementId">Statement ID from SQL map</param>
    /// <param name="parameters">Query parameters</param>
    /// <returns>Number of rows affected</returns>
    public static async Task<int> StatementExecuteAsync(
        this ISqlMapConnection connection,
        string statementId,
        object? parameters = null)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));

        if (string.IsNullOrWhiteSpace(statementId))
            throw new ArgumentException("Statement ID cannot be null or empty", nameof(statementId));

        var statement = connection.SqlMapConfig.GetStatement(statementId);
        if (statement == null)
        {
            throw new InvalidOperationException(
                $"Statement '{statementId}' not found in SQL map for DAO '{connection.DaoName}'");
        }

        return await connection.InnerConnection.ExecuteAsync(statement.CommandText, parameters);
    }

    /// <summary>
    /// Executes a query statement and returns the first result or default
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="connection">SQL connection</param>
    /// <param name="statementId">Statement ID from SQL map</param>
    /// <param name="parameters">Query parameters</param>
    /// <returns>First result or default(T)</returns>
    public static async Task<T?> StatementExecuteFirstAsync<T>(
        this ISqlMapConnection connection,
        string statementId,
        object? parameters = null)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));

        if (string.IsNullOrWhiteSpace(statementId))
            throw new ArgumentException("Statement ID cannot be null or empty", nameof(statementId));

        var statement = connection.SqlMapConfig.GetStatement(statementId);
        if (statement == null)
        {
            throw new InvalidOperationException(
                $"Statement '{statementId}' not found in SQL map for DAO '{connection.DaoName}'");
        }

        return await connection.InnerConnection.QueryFirstOrDefaultAsync<T>(statement.CommandText, parameters);
    }

    /// <summary>
    /// Executes multiple statements in a transaction
    /// </summary>
    /// <param name="connection">SQL connection</param>
    /// <param name="action">Action to execute within transaction</param>
    public static async Task ExecuteInTransactionAsync(
        this ISqlMapConnection connection,
        Func<ISqlMapConnection, Task> action)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));

        if (action == null)
            throw new ArgumentNullException(nameof(action));

        using var transaction = connection.BeginTransaction();

        try
        {
            await action(connection);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Executes multiple statements in a transaction with result
    /// </summary>
    /// <typeparam name="TResult">Result type</typeparam>
    /// <param name="connection">SQL connection</param>
    /// <param name="func">Function to execute within transaction</param>
    /// <returns>Transaction result</returns>
    public static async Task<TResult> ExecuteInTransactionAsync<TResult>(
        this ISqlMapConnection connection,
        Func<ISqlMapConnection, Task<TResult>> func)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));

        if (func == null)
            throw new ArgumentNullException(nameof(func));

        using var transaction = connection.BeginTransaction();

        try
        {
            var result = await func(connection);
            transaction.Commit();
            return result;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
