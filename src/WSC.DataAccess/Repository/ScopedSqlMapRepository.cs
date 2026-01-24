using Microsoft.Extensions.Logging;
using WSC.DataAccess.Core;
using WSC.DataAccess.Mapping;

namespace WSC.DataAccess.Repository;

/// <summary>
/// Base repository với SQL map file riêng biệt
/// Mỗi repository chỉ load SQL map file của nó, tránh conflict
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public abstract class ScopedSqlMapRepository<T> where T : class
{
    protected readonly IDbSessionFactory SessionFactory;
    protected readonly SqlMapper SqlMapper;
    protected readonly ILogger? Logger;

    /// <summary>
    /// Constructor với SQL map file cụ thể
    /// </summary>
    /// <param name="sessionFactory">Session factory</param>
    /// <param name="sqlMapFile">Đường dẫn đến SQL map file riêng (ví dụ: "SqlMaps/DAO005.xml")</param>
    /// <param name="loggerConfig">Logger cho SqlMapConfig (optional)</param>
    /// <param name="loggerMapper">Logger cho SqlMapper (optional)</param>
    /// <param name="logger">Logger cho Repository (optional)</param>
    protected ScopedSqlMapRepository(
        IDbSessionFactory sessionFactory,
        string sqlMapFile,
        ILogger<SqlMapConfig>? loggerConfig = null,
        ILogger<SqlMapper>? loggerMapper = null,
        ILogger? logger = null)
    {
        SessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        Logger = logger;

        // Tạo SqlMapConfig riêng cho repository này - chỉ load file này
        var config = SqlMapConfigBuilder.FromFile(sqlMapFile, loggerConfig);
        SqlMapper = new SqlMapper(config, loggerMapper ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SqlMapper>.Instance);
    }

    /// <summary>
    /// Constructor với nhiều SQL map files
    /// </summary>
    protected ScopedSqlMapRepository(
        IDbSessionFactory sessionFactory,
        string[] sqlMapFiles,
        ILogger<SqlMapConfig>? loggerConfig = null,
        ILogger<SqlMapper>? loggerMapper = null,
        ILogger? logger = null)
    {
        SessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        Logger = logger;

        // Tạo SqlMapConfig riêng cho repository này - chỉ load các files này
        var config = SqlMapConfigBuilder.FromFiles(loggerConfig, sqlMapFiles);
        SqlMapper = new SqlMapper(config, loggerMapper ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SqlMapper>.Instance);
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
