using WSC.DataAccess.Core;
using WSC.DataAccess.Mapping;
using Microsoft.Extensions.Logging;

namespace WSC.DataAccess.Repository;

/// <summary>
/// Base repository using SQL Map configuration (IBatis-style)
/// </summary>
public abstract class SqlMapRepository<T> where T : class
{
    protected readonly IDbSessionFactory SessionFactory;
    protected readonly SqlMapper SqlMapper;
    protected readonly ILogger? Logger;

    protected SqlMapRepository(IDbSessionFactory sessionFactory, SqlMapper sqlMapper, ILogger? logger = null)
    {
        SessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        SqlMapper = sqlMapper ?? throw new ArgumentNullException(nameof(sqlMapper));
        Logger = logger;
    }

    /// <summary>
    /// Query for a list using statement ID
    /// </summary>
    protected async Task<IEnumerable<T>> QueryListAsync(string statementId, object? parameters = null)
    {
        Logger?.LogDebug("Repository QueryListAsync - Entity: {EntityType}, StatementId: {StatementId}",
            typeof(T).Name, statementId);

        try
        {
            using var session = SessionFactory.OpenSession();
            return await SqlMapper.QueryAsync<T>(session, statementId, parameters);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Repository QueryListAsync failed - Entity: {EntityType}, StatementId: {StatementId}",
                typeof(T).Name, statementId);
            throw;
        }
    }

    /// <summary>
    /// Query for a single object using statement ID
    /// </summary>
    protected async Task<T?> QuerySingleAsync(string statementId, object? parameters = null)
    {
        Logger?.LogDebug("Repository QuerySingleAsync - Entity: {EntityType}, StatementId: {StatementId}",
            typeof(T).Name, statementId);

        try
        {
            using var session = SessionFactory.OpenSession();
            return await SqlMapper.QuerySingleAsync<T>(session, statementId, parameters);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Repository QuerySingleAsync failed - Entity: {EntityType}, StatementId: {StatementId}",
                typeof(T).Name, statementId);
            throw;
        }
    }

    /// <summary>
    /// Execute insert, update, delete using statement ID
    /// </summary>
    protected async Task<int> ExecuteAsync(string statementId, object? parameters = null)
    {
        Logger?.LogDebug("Repository ExecuteAsync - Entity: {EntityType}, StatementId: {StatementId}",
            typeof(T).Name, statementId);

        try
        {
            using var session = SessionFactory.OpenSession();
            return await SqlMapper.ExecuteAsync(session, statementId, parameters);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Repository ExecuteAsync failed - Entity: {EntityType}, StatementId: {StatementId}",
                typeof(T).Name, statementId);
            throw;
        }
    }

    /// <summary>
    /// Execute within a transaction
    /// </summary>
    protected async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<DbSession, Task<TResult>> operation)
    {
        Logger?.LogDebug("Repository ExecuteInTransactionAsync - Entity: {EntityType}", typeof(T).Name);

        using var session = SessionFactory.OpenSession();
        session.BeginTransaction();

        try
        {
            var result = await operation(session);
            session.Commit();
            Logger?.LogDebug("Repository transaction completed successfully - Entity: {EntityType}", typeof(T).Name);
            return result;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Repository transaction failed - Entity: {EntityType}", typeof(T).Name);
            session.Rollback();
            throw;
        }
    }
}
