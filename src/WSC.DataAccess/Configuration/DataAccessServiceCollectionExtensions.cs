using Microsoft.Extensions.Configuration;
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

        // Register SQL service (iBatis.NET style API)
        services.AddScoped<ISql, SqlService>();

        return services;
    }

    /// <summary>
    /// Adds WSC Data Access services with automatic connection string loading from IConfiguration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Configuration instance to load connection strings from</param>
    /// <param name="defaultConnectionName">Name of the default connection after suffix removal (default: "Default", which maps to "DefaultConnection" in appsettings.json)</param>
    /// <param name="configure">Optional configuration action</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddWscDataAccess(
        this IServiceCollection services,
        IConfiguration configuration,
        string defaultConnectionName = "Default",
        Action<DataAccessOptions>? configure = null)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var options = new DataAccessOptions();

        // Auto-load all connection strings from configuration
        var connectionStringsSection = configuration.GetSection("ConnectionStrings");
        string? defaultConnectionString = null;

        if (connectionStringsSection != null)
        {
            foreach (var connectionString in connectionStringsSection.GetChildren())
            {
                var name = connectionString.Key;
                var value = connectionString.Value;

                if (!string.IsNullOrWhiteSpace(value))
                {
                    // Remove "Connection" suffix if present to make names shorter
                    // "DefaultConnection" -> "Default", "HISConnection" -> "HIS"
                    var cleanName = name.EndsWith("Connection", StringComparison.OrdinalIgnoreCase)
                        ? name.Substring(0, name.Length - "Connection".Length)
                        : name;

                    options.NamedConnectionStrings[cleanName] = value;

                    // Track the default connection string
                    if (cleanName.Equals(defaultConnectionName, StringComparison.OrdinalIgnoreCase))
                    {
                        defaultConnectionString = value;
                    }
                }
            }
        }

        // Validate default connection was found
        if (string.IsNullOrWhiteSpace(defaultConnectionString))
            throw new ArgumentException($"Connection string '{defaultConnectionName}' not found in configuration (looking for '{defaultConnectionName}Connection' or '{defaultConnectionName}')", nameof(defaultConnectionName));

        // Allow user to override or add more options
        configure?.Invoke(options);

        // Register connection factory with default connection
        services.AddSingleton<IDbConnectionFactory>(sp => new SqlConnectionFactory(defaultConnectionString));

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

        // Register SQL service (iBatis.NET style API)
        services.AddScoped<ISql, SqlService>();

        return services;
    }

    /// <summary>
    /// Adds WSC Data Access services with a dictionary of connection strings
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionStrings">Dictionary of connection strings (key: connection name, value: connection string)</param>
    /// <param name="defaultConnectionName">Name of the default connection (default: "Default")</param>
    /// <param name="configure">Optional configuration action</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddWscDataAccess(
        this IServiceCollection services,
        Dictionary<string, string> connectionStrings,
        string defaultConnectionName = "Default",
        Action<DataAccessOptions>? configure = null)
    {
        if (connectionStrings == null || connectionStrings.Count == 0)
            throw new ArgumentException("Connection strings dictionary cannot be null or empty", nameof(connectionStrings));

        if (!connectionStrings.TryGetValue(defaultConnectionName, out var defaultConnectionString))
            throw new ArgumentException($"Default connection '{defaultConnectionName}' not found in connection strings", nameof(defaultConnectionName));

        var options = new DataAccessOptions();

        // Add all connection strings to options
        foreach (var kvp in connectionStrings)
        {
            options.NamedConnectionStrings[kvp.Key] = kvp.Value;
        }

        // Allow user to override or add more options
        configure?.Invoke(options);

        // Register connection factory with default connection
        services.AddSingleton<IDbConnectionFactory>(sp => new SqlConnectionFactory(defaultConnectionString));

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

        // Register SQL service (iBatis.NET style API)
        services.AddScoped<ISql, SqlService>();

        return services;
    }

    /// <summary>
    /// Adds WSC Data Access services with connection strings from IConfigurationSection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionStringsSection">Configuration section containing connection strings (e.g., configuration.GetSection("ConnectionStrings"))</param>
    /// <param name="defaultConnectionName">Name of the default connection after suffix removal (default: "Default", which maps to "DefaultConnection" in config)</param>
    /// <param name="configure">Optional configuration action</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddWscDataAccess(
        this IServiceCollection services,
        IConfigurationSection connectionStringsSection,
        string defaultConnectionName = "Default",
        Action<DataAccessOptions>? configure = null)
    {
        if (connectionStringsSection == null)
            throw new ArgumentNullException(nameof(connectionStringsSection));

        // Extract connection strings from section
        var connectionStrings = new Dictionary<string, string>();
        foreach (var child in connectionStringsSection.GetChildren())
        {
            var name = child.Key;
            var value = child.Value;

            if (!string.IsNullOrWhiteSpace(value))
            {
                // Remove "Connection" suffix if present to make names shorter
                // "DefaultConnection" -> "Default", "HISConnection" -> "HIS"
                var cleanName = name.EndsWith("Connection", StringComparison.OrdinalIgnoreCase)
                    ? name.Substring(0, name.Length - "Connection".Length)
                    : name;

                connectionStrings[cleanName] = value;
            }
        }

        if (connectionStrings.Count == 0)
            throw new ArgumentException("Configuration section does not contain any connection strings", nameof(connectionStringsSection));

        if (!connectionStrings.TryGetValue(defaultConnectionName, out var defaultConnectionString))
            throw new ArgumentException($"Default connection '{defaultConnectionName}' not found in configuration section (looking for '{defaultConnectionName}Connection' or '{defaultConnectionName}')", nameof(defaultConnectionName));

        var options = new DataAccessOptions();

        // Add all connection strings to options
        foreach (var kvp in connectionStrings)
        {
            options.NamedConnectionStrings[kvp.Key] = kvp.Value;
        }

        // Allow user to override or add more options
        configure?.Invoke(options);

        // Register connection factory with default connection
        services.AddSingleton<IDbConnectionFactory>(sp => new SqlConnectionFactory(defaultConnectionString));

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

        // Register SQL service (iBatis.NET style API)
        services.AddScoped<ISql, SqlService>();

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

    /// <summary>
    /// Auto-discover và register tất cả SQL map files trong thư mục
    /// </summary>
    /// <param name="directory">Thư mục chứa SQL map files (ví dụ: "SqlMaps")</param>
    /// <param name="connectionName">Connection name (mặc định: "Default")</param>
    /// <param name="searchPattern">Pattern để filter files (mặc định: "*.xml")</param>
    public void AutoDiscoverSqlMaps(string directory, string? connectionName = null, string searchPattern = "*.xml")
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Directory cannot be null or empty", nameof(directory));

        connectionName ??= SqlMapProvider.DEFAULT_CONNECTION;

        // Check if directory exists
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Directory '{directory}' not found for SQL map auto-discovery");
        }

        // Find all XML files
        var xmlFiles = Directory.GetFiles(directory, searchPattern, SearchOption.TopDirectoryOnly);

        if (xmlFiles.Length == 0)
        {
            throw new InvalidOperationException($"No SQL map files found in '{directory}' matching pattern '{searchPattern}'");
        }

        // Register each file
        foreach (var filePath in xmlFiles)
        {
            // Extract DAO name from filename (remove extension)
            var fileName = Path.GetFileNameWithoutExtension(filePath);

            // Use relative path from current directory
            var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), filePath);

            SqlMapProvider.AddFile(fileName, relativePath, connectionName);
        }
    }
}
