using Dapper;
using WSC.DataAccess.Core;
using System.Data;

namespace WSC.DataAccess.Mapping;

/// <summary>
/// SQL Mapper - executes SQL statements defined in configuration (IBatis-style)
/// </summary>
public class SqlMapper
{
    private readonly SqlMapConfig _config;

    public SqlMapper(SqlMapConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Query for a list of objects
    /// </summary>
    public async Task<IEnumerable<T>> QueryAsync<T>(DbSession session, string statementId, object? parameters = null)
    {
        var statement = GetStatement(statementId);
        ValidateStatementType(statement, SqlStatementType.Select);

        return await session.Connection.QueryAsync<T>(
            statement.CommandText,
            parameters,
            session.Transaction,
            commandTimeout: statement.CommandTimeout);
    }

    /// <summary>
    /// Query for a single object
    /// </summary>
    public async Task<T?> QuerySingleAsync<T>(DbSession session, string statementId, object? parameters = null)
    {
        var statement = GetStatement(statementId);
        ValidateStatementType(statement, SqlStatementType.Select);

        return await session.Connection.QueryFirstOrDefaultAsync<T>(
            statement.CommandText,
            parameters,
            session.Transaction,
            commandTimeout: statement.CommandTimeout);
    }

    /// <summary>
    /// Execute an insert, update or delete statement
    /// </summary>
    public async Task<int> ExecuteAsync(DbSession session, string statementId, object? parameters = null)
    {
        var statement = GetStatement(statementId);

        return await session.Connection.ExecuteAsync(
            statement.CommandText,
            parameters,
            session.Transaction,
            commandTimeout: statement.CommandTimeout);
    }

    /// <summary>
    /// Execute a stored procedure
    /// </summary>
    public async Task<IEnumerable<T>> ExecuteProcedureAsync<T>(DbSession session, string statementId, object? parameters = null)
    {
        var statement = GetStatement(statementId);
        ValidateStatementType(statement, SqlStatementType.Procedure);

        return await session.Connection.QueryAsync<T>(
            statement.CommandText,
            parameters,
            session.Transaction,
            commandType: CommandType.StoredProcedure,
            commandTimeout: statement.CommandTimeout);
    }

    /// <summary>
    /// Execute a query and return a scalar value
    /// </summary>
    public async Task<T?> ExecuteScalarAsync<T>(DbSession session, string statementId, object? parameters = null)
    {
        var statement = GetStatement(statementId);

        return await session.Connection.ExecuteScalarAsync<T>(
            statement.CommandText,
            parameters,
            session.Transaction,
            commandTimeout: statement.CommandTimeout);
    }

    private SqlStatement GetStatement(string statementId)
    {
        var statement = _config.GetStatement(statementId);
        if (statement == null)
        {
            throw new InvalidOperationException($"SQL statement '{statementId}' not found in configuration");
        }
        return statement;
    }

    private void ValidateStatementType(SqlStatement statement, SqlStatementType expectedType)
    {
        if (statement.StatementType != expectedType && statement.StatementType != SqlStatementType.Unknown)
        {
            throw new InvalidOperationException(
                $"Statement '{statement.Id}' is of type '{statement.StatementType}' but '{expectedType}' was expected");
        }
    }
}
