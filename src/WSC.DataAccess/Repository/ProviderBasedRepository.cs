using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Core;
using WSC.DataAccess.Mapping;

namespace WSC.DataAccess.Repository;

/// <summary>
/// Repository sử dụng SqlMapProvider để lấy file
/// Giống provider pattern trong MrFu.Smartcheck
/// </summary>
public abstract class ProviderBasedRepository<T> where T : class
{
    protected readonly IDbSessionFactory SessionFactory;
    protected readonly SqlMapper SqlMapper;
    protected readonly SqlMapProvider Provider;
    protected readonly ILogger? Logger;

    /// <summary>
    /// Constructor với SqlMapProvider key
    /// </summary>
    /// <param name="sessionFactory">Session factory</param>
    /// <param name="provider">SQL map provider</param>
    /// <param name="mapKey">Key của SQL map trong provider (ví dụ: "Order", "Customer")</param>
    /// <param name="loggerConfig">Logger for config</param>
    /// <param name="loggerMapper">Logger for mapper</param>
    /// <param name="logger">Logger for repository</param>
    protected ProviderBasedRepository(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider,
        string mapKey,
        ILogger<SqlMapConfig>? loggerConfig = null,
        ILogger<SqlMapper>? loggerMapper = null,
        ILogger? logger = null)
    {
        SessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        Logger = logger;

        // Lấy file path từ provider
        var filePath = provider.GetFilePath(mapKey);
        if (string.IsNullOrEmpty(filePath))
        {
            throw new InvalidOperationException(
                $"SQL map key '{mapKey}' not found in provider. " +
                $"Please register it in ConfigureSqlMaps(). " +
                $"Example: provider.AddFile(\"{mapKey}\", \"SqlMaps/YourFile.xml\")");
        }

        // Tạo SqlMapConfig từ file
        var config = SqlMapConfigBuilder.FromFile(filePath, loggerConfig);
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
