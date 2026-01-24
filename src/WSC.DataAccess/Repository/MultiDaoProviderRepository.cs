using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Core;
using WSC.DataAccess.Mapping;

namespace WSC.DataAccess.Repository;

/// <summary>
/// Repository sử dụng SqlMapProvider để load NHIỀU DAO files
/// Giống provider pattern trong MrFu.Smartcheck, hỗ trợ services phức tạp cần nhiều domains
/// </summary>
/// <typeparam name="T">Entity type (có thể dùng dynamic cho mixed types)</typeparam>
public abstract class MultiDaoProviderRepository<T> where T : class
{
    protected readonly IDbSessionFactory SessionFactory;
    protected readonly SqlMapper SqlMapper;
    protected readonly SqlMapProvider Provider;
    protected readonly ILogger? Logger;

    /// <summary>
    /// Constructor với nhiều SqlMapProvider keys (sử dụng default connection)
    /// </summary>
    /// <param name="sessionFactory">Session factory</param>
    /// <param name="provider">SQL map provider</param>
    /// <param name="mapKeys">Danh sách keys của SQL maps trong provider (ví dụ: ["DAO000", "DAO001", "DAO002"])</param>
    /// <param name="loggerConfig">Logger for config</param>
    /// <param name="loggerMapper">Logger for mapper</param>
    /// <param name="logger">Logger for repository</param>
    protected MultiDaoProviderRepository(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider,
        string[] mapKeys,
        ILogger<SqlMapConfig>? loggerConfig = null,
        ILogger<SqlMapper>? loggerMapper = null,
        ILogger? logger = null)
        : this(sessionFactory, provider, mapKeys, SqlMapProvider.DEFAULT_CONNECTION, loggerConfig, loggerMapper, logger)
    {
    }

    /// <summary>
    /// Constructor với nhiều SqlMapProvider keys và named connection
    /// </summary>
    /// <param name="sessionFactory">Session factory</param>
    /// <param name="provider">SQL map provider</param>
    /// <param name="mapKeys">Danh sách keys của SQL maps trong provider</param>
    /// <param name="connectionName">Tên connection (ví dụ: "Connection_1", "Connection_2")</param>
    /// <param name="loggerConfig">Logger for config</param>
    /// <param name="loggerMapper">Logger for mapper</param>
    /// <param name="logger">Logger for repository</param>
    protected MultiDaoProviderRepository(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider,
        string[] mapKeys,
        string connectionName,
        ILogger<SqlMapConfig>? loggerConfig = null,
        ILogger<SqlMapper>? loggerMapper = null,
        ILogger? logger = null)
    {
        SessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        Logger = logger;

        if (mapKeys == null || mapKeys.Length == 0)
        {
            throw new ArgumentException("At least one map key is required", nameof(mapKeys));
        }

        // Resolve tất cả file paths từ provider
        var filePaths = new List<string>();
        var missingKeys = new List<string>();

        foreach (var key in mapKeys)
        {
            var filePath = provider.GetFilePath(key, connectionName);
            if (string.IsNullOrEmpty(filePath))
            {
                missingKeys.Add(key);
            }
            else
            {
                filePaths.Add(filePath);
            }
        }

        // Kiểm tra xem có key nào không tìm thấy không
        if (missingKeys.Any())
        {
            throw new InvalidOperationException(
                $"SQL map keys not found in provider for connection '{connectionName}': {string.Join(", ", missingKeys)}. " +
                $"Please register them in ConfigureSqlMaps(). " +
                $"Example: provider.AddFile(\"{missingKeys[0]}\", \"SqlMaps/YourFile.xml\", \"{connectionName}\")");
        }

        Logger?.LogDebug(
            "MultiDaoProviderRepository initialized with {Count} map keys from connection '{ConnectionName}': {MapKeys}",
            mapKeys.Length, connectionName, string.Join(", ", mapKeys));

        Logger?.LogDebug(
            "Loading {Count} SQL map files: {FilePaths}",
            filePaths.Count, string.Join(", ", filePaths));

        // Tạo SqlMapConfig từ nhiều files
        var config = SqlMapConfigBuilder.FromFiles(loggerConfig, filePaths.ToArray());
        SqlMapper = new SqlMapper(config, loggerMapper ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SqlMapper>.Instance);
    }

    /// <summary>
    /// Query for a list using statement ID (sử dụng default connection)
    /// </summary>
    protected async Task<IEnumerable<T>> QueryListAsync(string statementId, object? parameters = null)
    {
        return await QueryListAsync(statementId, parameters, null);
    }

    /// <summary>
    /// Query for a list using statement ID với named connection
    /// </summary>
    /// <param name="statementId">Statement ID</param>
    /// <param name="parameters">Parameters</param>
    /// <param name="connectionName">Named connection (null = default)</param>
    protected async Task<IEnumerable<T>> QueryListAsync(string statementId, object? parameters, string? connectionName)
    {
        Logger?.LogDebug("MultiDaoRepository QueryListAsync - Entity: {EntityType}, StatementId: {StatementId}, Connection: {ConnectionName}",
            typeof(T).Name, statementId, connectionName ?? "Default");

        try
        {
            using var session = string.IsNullOrEmpty(connectionName)
                ? SessionFactory.OpenSession()
                : SessionFactory.OpenSession(connectionName);

            return await SqlMapper.QueryAsync<T>(session, statementId, parameters);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "MultiDaoRepository QueryListAsync failed - Entity: {EntityType}, StatementId: {StatementId}, Connection: {ConnectionName}",
                typeof(T).Name, statementId, connectionName ?? "Default");
            throw;
        }
    }

    /// <summary>
    /// Query for a list with specific type using statement ID (sử dụng default connection)
    /// Hỗ trợ query mixed types (ví dụ: repository<dynamic> nhưng query về User, Product, v.v.)
    /// </summary>
    protected async Task<IEnumerable<TResult>> QueryListAsync<TResult>(string statementId, object? parameters = null)
    {
        return await QueryListAsync<TResult>(statementId, parameters, null);
    }

    /// <summary>
    /// Query for a list with specific type using statement ID với named connection
    /// </summary>
    protected async Task<IEnumerable<TResult>> QueryListAsync<TResult>(string statementId, object? parameters, string? connectionName)
    {
        Logger?.LogDebug("MultiDaoRepository QueryListAsync - ResultType: {ResultType}, StatementId: {StatementId}, Connection: {ConnectionName}",
            typeof(TResult).Name, statementId, connectionName ?? "Default");

        try
        {
            using var session = string.IsNullOrEmpty(connectionName)
                ? SessionFactory.OpenSession()
                : SessionFactory.OpenSession(connectionName);

            return await SqlMapper.QueryAsync<TResult>(session, statementId, parameters);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "MultiDaoRepository QueryListAsync failed - ResultType: {ResultType}, StatementId: {StatementId}, Connection: {ConnectionName}",
                typeof(TResult).Name, statementId, connectionName ?? "Default");
            throw;
        }
    }

    /// <summary>
    /// Query for a single object using statement ID (sử dụng default connection)
    /// </summary>
    protected async Task<T?> QuerySingleAsync(string statementId, object? parameters = null)
    {
        return await QuerySingleAsync(statementId, parameters, null);
    }

    /// <summary>
    /// Query for a single object using statement ID với named connection
    /// </summary>
    protected async Task<T?> QuerySingleAsync(string statementId, object? parameters, string? connectionName)
    {
        Logger?.LogDebug("MultiDaoRepository QuerySingleAsync - Entity: {EntityType}, StatementId: {StatementId}, Connection: {ConnectionName}",
            typeof(T).Name, statementId, connectionName ?? "Default");

        try
        {
            using var session = string.IsNullOrEmpty(connectionName)
                ? SessionFactory.OpenSession()
                : SessionFactory.OpenSession(connectionName);

            return await SqlMapper.QuerySingleAsync<T>(session, statementId, parameters);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "MultiDaoRepository QuerySingleAsync failed - Entity: {EntityType}, StatementId: {StatementId}, Connection: {ConnectionName}",
                typeof(T).Name, statementId, connectionName ?? "Default");
            throw;
        }
    }

    /// <summary>
    /// Query for a single object with specific type using statement ID (sử dụng default connection)
    /// Hỗ trợ query mixed types
    /// </summary>
    protected async Task<TResult?> QuerySingleAsync<TResult>(string statementId, object? parameters = null)
    {
        return await QuerySingleAsync<TResult>(statementId, parameters, null);
    }

    /// <summary>
    /// Query for a single object with specific type using statement ID với named connection
    /// </summary>
    protected async Task<TResult?> QuerySingleAsync<TResult>(string statementId, object? parameters, string? connectionName)
    {
        Logger?.LogDebug("MultiDaoRepository QuerySingleAsync - ResultType: {ResultType}, StatementId: {StatementId}, Connection: {ConnectionName}",
            typeof(TResult).Name, statementId, connectionName ?? "Default");

        try
        {
            using var session = string.IsNullOrEmpty(connectionName)
                ? SessionFactory.OpenSession()
                : SessionFactory.OpenSession(connectionName);

            return await SqlMapper.QuerySingleAsync<TResult>(session, statementId, parameters);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "MultiDaoRepository QuerySingleAsync failed - ResultType: {ResultType}, StatementId: {StatementId}, Connection: {ConnectionName}",
                typeof(TResult).Name, statementId, connectionName ?? "Default");
            throw;
        }
    }

    /// <summary>
    /// Execute insert, update, delete using statement ID (sử dụng default connection)
    /// </summary>
    protected async Task<int> ExecuteAsync(string statementId, object? parameters = null)
    {
        return await ExecuteAsync(statementId, parameters, null);
    }

    /// <summary>
    /// Execute insert, update, delete using statement ID với named connection
    /// </summary>
    protected async Task<int> ExecuteAsync(string statementId, object? parameters, string? connectionName)
    {
        Logger?.LogDebug("MultiDaoRepository ExecuteAsync - Entity: {EntityType}, StatementId: {StatementId}, Connection: {ConnectionName}",
            typeof(T).Name, statementId, connectionName ?? "Default");

        try
        {
            using var session = string.IsNullOrEmpty(connectionName)
                ? SessionFactory.OpenSession()
                : SessionFactory.OpenSession(connectionName);

            return await SqlMapper.ExecuteAsync(session, statementId, parameters);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "MultiDaoRepository ExecuteAsync failed - Entity: {EntityType}, StatementId: {StatementId}, Connection: {ConnectionName}",
                typeof(T).Name, statementId, connectionName ?? "Default");
            throw;
        }
    }

    /// <summary>
    /// Execute within a transaction
    /// </summary>
    protected async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<DbSession, Task<TResult>> operation)
    {
        Logger?.LogDebug("MultiDaoRepository ExecuteInTransactionAsync - Entity: {EntityType}", typeof(T).Name);

        using var session = SessionFactory.OpenSession();
        session.BeginTransaction();

        try
        {
            var result = await operation(session);
            session.Commit();
            Logger?.LogDebug("MultiDaoRepository transaction completed successfully - Entity: {EntityType}", typeof(T).Name);
            return result;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "MultiDaoRepository transaction failed - Entity: {EntityType}", typeof(T).Name);
            session.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Execute within a transaction (void return)
    /// </summary>
    protected async Task ExecuteInTransactionAsync(
        Func<DbSession, Task> operation)
    {
        Logger?.LogDebug("MultiDaoRepository ExecuteInTransactionAsync - Entity: {EntityType}", typeof(T).Name);

        using var session = SessionFactory.OpenSession();
        session.BeginTransaction();

        try
        {
            await operation(session);
            session.Commit();
            Logger?.LogDebug("MultiDaoRepository transaction completed successfully - Entity: {EntityType}", typeof(T).Name);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "MultiDaoRepository transaction failed - Entity: {EntityType}", typeof(T).Name);
            session.Rollback();
            throw;
        }
    }
}
