using Dapper;
using WSC.DataAccess.Core;
using System.Data;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace WSC.DataAccess.Mapping;

/// <summary>
/// SQL Mapper - executes SQL statements defined in configuration (IBatis-style)
/// </summary>
public class SqlMapper
{
    private readonly SqlMapConfig _config;
    private readonly ILogger<SqlMapper> _logger;

    public SqlMapper(SqlMapConfig config, ILogger<SqlMapper> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Query for a list of objects
    /// </summary>
    public async Task<IEnumerable<T>> QueryAsync<T>(DbSession session, string statementId, object? parameters = null)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Executing QueryAsync - StatementId: {StatementId}, Type: {ResultType}",
            statementId, typeof(T).Name);

        try
        {
            var statement = GetStatement(statementId);
            ValidateStatementType(statement, SqlStatementType.Select);

            var result = await session.Connection.QueryAsync<T>(
                statement.CommandText,
                parameters,
                session.Transaction,
                commandTimeout: statement.CommandTimeout);

            stopwatch.Stop();
            var resultList = result.ToList();
            _logger.LogInformation(
                "QueryAsync completed - StatementId: {StatementId}, ResultCount: {ResultCount}, Duration: {DurationMs}ms",
                statementId, resultList.Count, stopwatch.ElapsedMilliseconds);

            return resultList;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "QueryAsync failed - StatementId: {StatementId}, Duration: {DurationMs}ms",
                statementId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Query for a single object
    /// </summary>
    public async Task<T?> QuerySingleAsync<T>(DbSession session, string statementId, object? parameters = null)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Executing QuerySingleAsync - StatementId: {StatementId}, Type: {ResultType}",
            statementId, typeof(T).Name);

        try
        {
            var statement = GetStatement(statementId);
            ValidateStatementType(statement, SqlStatementType.Select);

            var result = await session.Connection.QueryFirstOrDefaultAsync<T>(
                statement.CommandText,
                parameters,
                session.Transaction,
                commandTimeout: statement.CommandTimeout);

            stopwatch.Stop();
            _logger.LogInformation(
                "QuerySingleAsync completed - StatementId: {StatementId}, Found: {Found}, Duration: {DurationMs}ms",
                statementId, result != null, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "QuerySingleAsync failed - StatementId: {StatementId}, Duration: {DurationMs}ms",
                statementId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Execute an insert, update or delete statement
    /// </summary>
    public async Task<int> ExecuteAsync(DbSession session, string statementId, object? parameters = null)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Executing ExecuteAsync - StatementId: {StatementId}", statementId);

        try
        {
            var statement = GetStatement(statementId);

            var rowsAffected = await session.Connection.ExecuteAsync(
                statement.CommandText,
                parameters,
                session.Transaction,
                commandTimeout: statement.CommandTimeout);

            stopwatch.Stop();
            _logger.LogInformation(
                "ExecuteAsync completed - StatementId: {StatementId}, RowsAffected: {RowsAffected}, Duration: {DurationMs}ms",
                statementId, rowsAffected, stopwatch.ElapsedMilliseconds);

            return rowsAffected;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "ExecuteAsync failed - StatementId: {StatementId}, Duration: {DurationMs}ms",
                statementId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Execute a stored procedure
    /// </summary>
    public async Task<IEnumerable<T>> ExecuteProcedureAsync<T>(DbSession session, string statementId, object? parameters = null)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Executing ExecuteProcedureAsync - StatementId: {StatementId}, Type: {ResultType}",
            statementId, typeof(T).Name);

        try
        {
            var statement = GetStatement(statementId);
            ValidateStatementType(statement, SqlStatementType.Procedure);

            var result = await session.Connection.QueryAsync<T>(
                statement.CommandText,
                parameters,
                session.Transaction,
                commandType: CommandType.StoredProcedure,
                commandTimeout: statement.CommandTimeout);

            stopwatch.Stop();
            var resultList = result.ToList();
            _logger.LogInformation(
                "ExecuteProcedureAsync completed - StatementId: {StatementId}, ResultCount: {ResultCount}, Duration: {DurationMs}ms",
                statementId, resultList.Count, stopwatch.ElapsedMilliseconds);

            return resultList;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "ExecuteProcedureAsync failed - StatementId: {StatementId}, Duration: {DurationMs}ms",
                statementId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Execute a query and return a scalar value
    /// </summary>
    public async Task<T?> ExecuteScalarAsync<T>(DbSession session, string statementId, object? parameters = null)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Executing ExecuteScalarAsync - StatementId: {StatementId}, Type: {ResultType}",
            statementId, typeof(T).Name);

        try
        {
            var statement = GetStatement(statementId);

            var result = await session.Connection.ExecuteScalarAsync<T>(
                statement.CommandText,
                parameters,
                session.Transaction,
                commandTimeout: statement.CommandTimeout);

            stopwatch.Stop();
            _logger.LogInformation(
                "ExecuteScalarAsync completed - StatementId: {StatementId}, Result: {Result}, Duration: {DurationMs}ms",
                statementId, result, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "ExecuteScalarAsync failed - StatementId: {StatementId}, Duration: {DurationMs}ms",
                statementId, stopwatch.ElapsedMilliseconds);
            throw;
        }
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
