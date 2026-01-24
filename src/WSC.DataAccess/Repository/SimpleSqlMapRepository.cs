using Microsoft.Extensions.Logging;
using WSC.DataAccess.Attributes;
using WSC.DataAccess.Core;
using WSC.DataAccess.Mapping;

namespace WSC.DataAccess.Repository;

/// <summary>
/// Simple repository - Tự động đọc SQL map file từ attribute
/// Cách dùng đơn giản nhất:
///
/// [SqlMapFile(SqlMapFiles.DAO005)]
/// public class OrderRepository : SimpleSqlMapRepository&lt;Order&gt; { }
/// </summary>
public abstract class SimpleSqlMapRepository<T> where T : class
{
    protected readonly IDbSessionFactory SessionFactory;
    protected readonly SqlMapper SqlMapper;
    protected readonly ILogger? Logger;

    /// <summary>
    /// Constructor - Tự động đọc SQL map file từ [SqlMapFile] attribute
    /// </summary>
    protected SimpleSqlMapRepository(
        IDbSessionFactory sessionFactory,
        ILogger<SqlMapConfig>? loggerConfig = null,
        ILogger<SqlMapper>? loggerMapper = null,
        ILogger? logger = null)
    {
        SessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        Logger = logger;

        // Tự động đọc SQL map file từ attribute
        var sqlMapFile = GetSqlMapFileFromAttribute();
        var sqlMapFiles = GetSqlMapFilesFromAttribute();

        SqlMapConfig config;

        if (sqlMapFiles != null && sqlMapFiles.Length > 0)
        {
            // Có [SqlMapFiles] attribute
            config = SqlMapConfigBuilder.FromFiles(loggerConfig, sqlMapFiles);
        }
        else if (sqlMapFile != null)
        {
            // Có [SqlMapFile] attribute
            config = SqlMapConfigBuilder.FromFile(sqlMapFile, loggerConfig);
        }
        else
        {
            throw new InvalidOperationException(
                $"Repository '{GetType().Name}' phải có [SqlMapFile] hoặc [SqlMapFiles] attribute. " +
                $"Ví dụ: [SqlMapFile(SqlMapFiles.DAO005)]");
        }

        SqlMapper = new SqlMapper(config, loggerMapper ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SqlMapper>.Instance);
    }

    private string? GetSqlMapFileFromAttribute()
    {
        var attribute = GetType().GetCustomAttributes(typeof(SqlMapFileAttribute), true)
            .FirstOrDefault() as SqlMapFileAttribute;
        return attribute?.FilePath;
    }

    private string[]? GetSqlMapFilesFromAttribute()
    {
        var attribute = GetType().GetCustomAttributes(typeof(SqlMapFilesAttribute), true)
            .FirstOrDefault() as SqlMapFilesAttribute;
        return attribute?.FilePaths;
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
