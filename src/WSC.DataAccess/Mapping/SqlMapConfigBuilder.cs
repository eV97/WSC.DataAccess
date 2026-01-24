using Microsoft.Extensions.Logging;

namespace WSC.DataAccess.Mapping;

/// <summary>
/// Builder để tạo SqlMapConfig với specific SQL map files
/// Sử dụng cho từng service/repository riêng biệt
/// </summary>
public class SqlMapConfigBuilder
{
    private readonly List<string> _sqlMapFiles = new();
    private readonly ILogger<SqlMapConfig>? _logger;

    public SqlMapConfigBuilder(ILogger<SqlMapConfig>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Thêm SQL map file vào builder
    /// </summary>
    public SqlMapConfigBuilder AddSqlMapFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"SQL map file not found: {filePath}");
        }

        _sqlMapFiles.Add(filePath);
        return this;
    }

    /// <summary>
    /// Thêm nhiều SQL map files
    /// </summary>
    public SqlMapConfigBuilder AddSqlMapFiles(params string[] filePaths)
    {
        foreach (var filePath in filePaths)
        {
            AddSqlMapFile(filePath);
        }
        return this;
    }

    /// <summary>
    /// Build SqlMapConfig với các files đã chỉ định
    /// </summary>
    public SqlMapConfig Build()
    {
        var config = new SqlMapConfig(_logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SqlMapConfig>.Instance);

        foreach (var filePath in _sqlMapFiles)
        {
            config.LoadFromXml(filePath);
        }

        return config;
    }

    /// <summary>
    /// Tạo SqlMapConfig từ một file duy nhất
    /// </summary>
    public static SqlMapConfig FromFile(string filePath, ILogger<SqlMapConfig>? logger = null)
    {
        return new SqlMapConfigBuilder(logger)
            .AddSqlMapFile(filePath)
            .Build();
    }

    /// <summary>
    /// Tạo SqlMapConfig từ nhiều files
    /// </summary>
    public static SqlMapConfig FromFiles(ILogger<SqlMapConfig>? logger = null, params string[] filePaths)
    {
        return new SqlMapConfigBuilder(logger)
            .AddSqlMapFiles(filePaths)
            .Build();
    }
}
