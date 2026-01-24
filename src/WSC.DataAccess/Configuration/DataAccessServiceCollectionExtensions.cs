using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WSC.DataAccess.Core;
using WSC.DataAccess.Mapping;

namespace WSC.DataAccess.Configuration;

/// <summary>
/// Extension methods for setting up data access services in an IServiceCollection
/// </summary>
public static class DataAccessServiceCollectionExtensions
{
    /// <summary>
    /// Adds WSC Data Access services to the specified IServiceCollection
    /// </summary>
    public static IServiceCollection AddWscDataAccess(
        this IServiceCollection services,
        string connectionString,
        Action<DataAccessOptions>? configure = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        var options = new DataAccessOptions();
        configure?.Invoke(options);

        // Register connection factory
        services.AddSingleton<IDbConnectionFactory>(sp => new SqlConnectionFactory(connectionString));

        // Register session factory
        services.AddSingleton<IDbSessionFactory>(sp =>
        {
            var connectionFactory = sp.GetRequiredService<IDbConnectionFactory>();
            var logger = sp.GetService<ILogger<DbSession>>();
            var sessionFactory = new DbSessionFactory(connectionFactory, options.NamedConnectionStrings, logger);
            return sessionFactory;
        });

        // Register SQL map provider (singleton để share across app)
        services.AddSingleton(options.SqlMapProvider);

        // Register SQL map configuration
        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<SqlMapConfig>>();
            var config = new SqlMapConfig(logger);

            // Load SQL map files from old way (backward compatibility)
            foreach (var sqlMapFile in options.SqlMapFiles)
            {
                config.LoadFromXml(sqlMapFile);
            }

            // Load SQL map files from provider (new way)
            var provider = sp.GetRequiredService<SqlMapProvider>();
            foreach (var filePath in provider.GetAllFilePaths())
            {
                config.LoadFromXml(filePath);
            }

            return config;
        });

        // Register SQL mapper
        services.AddSingleton<SqlMapper>(sp =>
        {
            var config = sp.GetRequiredService<SqlMapConfig>();
            var logger = sp.GetRequiredService<ILogger<SqlMapper>>();
            return new SqlMapper(config, logger);
        });

        return services;
    }

    /// <summary>
    /// Adds a named connection string
    /// </summary>
    public static IServiceCollection AddNamedConnection(
        this IServiceCollection services,
        string name,
        string connectionString)
    {
        // This is handled through DataAccessOptions
        return services;
    }
}

/// <summary>
/// Options for configuring WSC Data Access
/// </summary>
public class DataAccessOptions
{
    /// <summary>
    /// Named connection strings
    /// </summary>
    public Dictionary<string, string> NamedConnectionStrings { get; set; } = new();

    /// <summary>
    /// SQL Map XML files to load
    /// </summary>
    public List<string> SqlMapFiles { get; set; } = new();

    /// <summary>
    /// SQL Map Provider - Khai báo SQL maps như provider
    /// </summary>
    public SqlMapProvider SqlMapProvider { get; } = new();

    /// <summary>
    /// Adds a named connection string
    /// </summary>
    public void AddConnection(string name, string connectionString)
    {
        NamedConnectionStrings[name] = connectionString;
    }

    /// <summary>
    /// Adds a SQL map file
    /// </summary>
    public void AddSqlMapFile(string filePath)
    {
        SqlMapFiles.Add(filePath);
    }

    /// <summary>
    /// Configure SQL Map Provider (giống provider pattern trong MrFu.Smartcheck)
    /// </summary>
    public void ConfigureSqlMaps(Action<SqlMapProvider> configure)
    {
        configure?.Invoke(SqlMapProvider);
    }
}
