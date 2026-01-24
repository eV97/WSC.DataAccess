namespace WSC.DataAccess.Configuration;

/// <summary>
/// SQL Map Provider - Khai báo SQL map files cho project
/// Sử dụng giống provider pattern trong MrFu.Smartcheck
/// Hỗ trợ multiple named connections
/// </summary>
public class SqlMapProvider
{
    /// <summary>
    /// Danh sách SQL map files được khai báo
    /// </summary>
    public List<SqlMapFileRegistration> Files { get; } = new();

    /// <summary>
    /// Default connection name
    /// </summary>
    public const string DEFAULT_CONNECTION = "Default";

    /// <summary>
    /// Đăng ký 1 SQL map file (sử dụng default connection)
    /// </summary>
    public SqlMapProvider AddFile(string key, string filePath, string? description = null)
    {
        return AddFile(key, filePath, DEFAULT_CONNECTION, description);
    }

    /// <summary>
    /// Đăng ký 1 SQL map file với named connection
    /// </summary>
    /// <param name="key">Key để identify file (ví dụ: "Order", "Customer")</param>
    /// <param name="filePath">Đường dẫn đến file SQL map</param>
    /// <param name="connectionName">Tên connection (ví dụ: "Connection_1", "Connection_2")</param>
    /// <param name="description">Mô tả</param>
    public SqlMapProvider AddFile(string key, string filePath, string connectionName, string? description = null)
    {
        Files.Add(new SqlMapFileRegistration
        {
            Key = key,
            FilePath = filePath,
            ConnectionName = connectionName,
            Description = description
        });
        return this;
    }

    /// <summary>
    /// Đăng ký nhiều SQL map files (sử dụng default connection)
    /// </summary>
    public SqlMapProvider AddFiles(params (string Key, string FilePath, string? Description)[] files)
    {
        foreach (var (key, filePath, description) in files)
        {
            AddFile(key, filePath, DEFAULT_CONNECTION, description);
        }
        return this;
    }

    /// <summary>
    /// Đăng ký nhiều SQL map files với named connection
    /// </summary>
    public SqlMapProvider AddFiles(string connectionName, params (string Key, string FilePath, string? Description)[] files)
    {
        foreach (var (key, filePath, description) in files)
        {
            AddFile(key, filePath, connectionName, description);
        }
        return this;
    }

    /// <summary>
    /// Lấy file path theo key (từ default connection)
    /// </summary>
    public string? GetFilePath(string key)
    {
        return GetFilePath(key, DEFAULT_CONNECTION);
    }

    /// <summary>
    /// Lấy file path theo key và connection name
    /// </summary>
    public string? GetFilePath(string key, string connectionName)
    {
        return Files.FirstOrDefault(f => f.Key == key && f.ConnectionName == connectionName)?.FilePath;
    }

    /// <summary>
    /// Lấy registration info theo key
    /// </summary>
    public SqlMapFileRegistration? GetRegistration(string key)
    {
        return Files.FirstOrDefault(f => f.Key == key);
    }

    /// <summary>
    /// Lấy registration info theo key và connection name
    /// </summary>
    public SqlMapFileRegistration? GetRegistration(string key, string connectionName)
    {
        return Files.FirstOrDefault(f => f.Key == key && f.ConnectionName == connectionName);
    }

    /// <summary>
    /// Lấy tất cả file paths
    /// </summary>
    public string[] GetAllFilePaths()
    {
        return Files.Select(f => f.FilePath).Distinct().ToArray();
    }

    /// <summary>
    /// Lấy tất cả file paths theo connection name
    /// </summary>
    public string[] GetAllFilePaths(string connectionName)
    {
        return Files.Where(f => f.ConnectionName == connectionName)
                   .Select(f => f.FilePath)
                   .Distinct()
                   .ToArray();
    }

    /// <summary>
    /// Kiểm tra key có tồn tại không
    /// </summary>
    public bool HasFile(string key)
    {
        return Files.Any(f => f.Key == key);
    }

    /// <summary>
    /// Kiểm tra key có tồn tại trong connection name không
    /// </summary>
    public bool HasFile(string key, string connectionName)
    {
        return Files.Any(f => f.Key == key && f.ConnectionName == connectionName);
    }

    /// <summary>
    /// Lấy danh sách tất cả connection names
    /// </summary>
    public string[] GetAllConnectionNames()
    {
        return Files.Select(f => f.ConnectionName)
                   .Distinct()
                   .ToArray();
    }

    /// <summary>
    /// Lấy tất cả registrations theo connection name
    /// </summary>
    public IEnumerable<SqlMapFileRegistration> GetFilesByConnection(string connectionName)
    {
        return Files.Where(f => f.ConnectionName == connectionName);
    }
}

/// <summary>
/// Registration info cho SQL map file
/// </summary>
public class SqlMapFileRegistration
{
    /// <summary>
    /// Key để identify file (ví dụ: "Order", "Customer", "DAO005")
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn đến file (ví dụ: "SqlMaps/DAO005.xml")
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Tên connection (ví dụ: "Default", "Connection_1", "Connection_2")
    /// </summary>
    public string ConnectionName { get; set; } = SqlMapProvider.DEFAULT_CONNECTION;

    /// <summary>
    /// Mô tả file này dùng cho gì
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Timestamp khi đăng ký
    /// </summary>
    public DateTime RegisteredAt { get; set; } = DateTime.Now;
}
