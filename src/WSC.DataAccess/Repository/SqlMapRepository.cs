using WSC.DataAccess.Core;
using WSC.DataAccess.Mapping;

namespace WSC.DataAccess.Repository;

/// <summary>
/// Base repository using SQL Map configuration (IBatis-style)
/// </summary>
public abstract class SqlMapRepository<T> where T : class
{
    protected readonly IDbSessionFactory SessionFactory;
    protected readonly SqlMapper SqlMapper;

    protected SqlMapRepository(IDbSessionFactory sessionFactory, SqlMapper sqlMapper)
    {
        SessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        SqlMapper = sqlMapper ?? throw new ArgumentNullException(nameof(sqlMapper));
    }

    /// <summary>
    /// Query for a list using statement ID
    /// </summary>
    protected async Task<IEnumerable<T>> QueryListAsync(string statementId, object? parameters = null)
    {
        using var session = SessionFactory.OpenSession();
        return await SqlMapper.QueryAsync<T>(session, statementId, parameters);
    }

    /// <summary>
    /// Query for a single object using statement ID
    /// </summary>
    protected async Task<T?> QuerySingleAsync(string statementId, object? parameters = null)
    {
        using var session = SessionFactory.OpenSession();
        return await SqlMapper.QuerySingleAsync<T>(session, statementId, parameters);
    }

    /// <summary>
    /// Execute insert, update, delete using statement ID
    /// </summary>
    protected async Task<int> ExecuteAsync(string statementId, object? parameters = null)
    {
        using var session = SessionFactory.OpenSession();
        return await SqlMapper.ExecuteAsync(session, statementId, parameters);
    }

    /// <summary>
    /// Execute within a transaction
    /// </summary>
    protected async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<DbSession, Task<TResult>> operation)
    {
        using var session = SessionFactory.OpenSession();
        session.BeginTransaction();

        try
        {
            var result = await operation(session);
            session.Commit();
            return result;
        }
        catch
        {
            session.Rollback();
            throw;
        }
    }
}
