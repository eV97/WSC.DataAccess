using Dapper;
using WSC.DataAccess.Core;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace WSC.DataAccess.Repository;

/// <summary>
/// Base repository implementation with common CRUD operations
/// </summary>
public abstract class BaseRepository<T> : IRepository<T> where T : class
{
    protected readonly IDbSessionFactory SessionFactory;
    protected readonly string TableName;
    protected readonly string KeyColumn;

    protected BaseRepository(IDbSessionFactory sessionFactory)
    {
        SessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));

        // Get table name from attribute or class name
        var tableAttr = typeof(T).GetCustomAttribute<TableAttribute>();
        TableName = tableAttr?.Name ?? typeof(T).Name;

        // Get key column name (default to "Id")
        KeyColumn = "Id";
    }

    protected BaseRepository(IDbSessionFactory sessionFactory, string tableName, string keyColumn)
    {
        SessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        TableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
        KeyColumn = keyColumn ?? throw new ArgumentNullException(nameof(keyColumn));
    }

    /// <summary>
    /// Gets an entity by its ID
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(object id)
    {
        using var session = SessionFactory.OpenSession();
        var sql = $"SELECT * FROM {TableName} WHERE {KeyColumn} = @Id";
        return await session.Connection.QueryFirstOrDefaultAsync<T>(sql, new { Id = id });
    }

    /// <summary>
    /// Gets all entities
    /// </summary>
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        using var session = SessionFactory.OpenSession();
        var sql = $"SELECT * FROM {TableName}";
        return await session.Connection.QueryAsync<T>(sql);
    }

    /// <summary>
    /// Inserts a new entity
    /// </summary>
    public abstract Task<int> InsertAsync(T entity);

    /// <summary>
    /// Updates an existing entity
    /// </summary>
    public abstract Task<int> UpdateAsync(T entity);

    /// <summary>
    /// Deletes an entity by ID
    /// </summary>
    public virtual async Task<int> DeleteAsync(object id)
    {
        using var session = SessionFactory.OpenSession();
        var sql = $"DELETE FROM {TableName} WHERE {KeyColumn} = @Id";
        return await session.Connection.ExecuteAsync(sql, new { Id = id });
    }

    /// <summary>
    /// Executes a custom query
    /// </summary>
    protected async Task<IEnumerable<T>> QueryAsync(string sql, object? parameters = null)
    {
        using var session = SessionFactory.OpenSession();
        return await session.Connection.QueryAsync<T>(sql, parameters);
    }

    /// <summary>
    /// Executes a custom command
    /// </summary>
    protected async Task<int> ExecuteAsync(string sql, object? parameters = null)
    {
        using var session = SessionFactory.OpenSession();
        return await session.Connection.ExecuteAsync(sql, parameters);
    }

    /// <summary>
    /// Executes within a transaction
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
