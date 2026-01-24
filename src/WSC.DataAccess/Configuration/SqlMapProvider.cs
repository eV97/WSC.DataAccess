namespace WSC.DataAccess.Configuration;

/// <summary>
/// SQL Map Provider - Khai báo SQL map files cho project
/// Sử dụng giống provider pattern trong MrFu.Smartcheck
/// </summary>
public class SqlMapProvider
{
    /// <summary>
    /// Danh sách SQL map files được khai báo
    /// </summary>
    public List<SqlMapFileRegistration> Files { get; } = new();

    /// <summary>
    /// Đăng ký 1 SQL map file
    /// </summary>
    public SqlMapProvider AddFile(string key, string filePath, string? description = null)
    {
        Files.Add(new SqlMapFileRegistration
        {
            Key = key,
            FilePath = filePath,
            Description = description
        });
        return this;
    }

    /// <summary>
    /// Đăng ký nhiều SQL map files
    /// </summary>
    public SqlMapProvider AddFiles(params (string Key, string FilePath, string? Description)[] files)
    {
        foreach (var (key, filePath, description) in files)
        {
            AddFile(key, filePath, description);
        }
        return this;
    }

    /// <summary>
    /// Lấy file path theo key
    /// </summary>
    public string? GetFilePath(string key)
    {
        return Files.FirstOrDefault(f => f.Key == key)?.FilePath;
    }

    /// <summary>
    /// Lấy tất cả file paths
    /// </summary>
    public string[] GetAllFilePaths()
    {
        return Files.Select(f => f.FilePath).ToArray();
    }

    /// <summary>
    /// Kiểm tra key có tồn tại không
    /// </summary>
    public bool HasFile(string key)
    {
        return Files.Any(f => f.Key == key);
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
    /// Mô tả file này dùng cho gì
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Timestamp khi đăng ký
    /// </summary>
    public DateTime RegisteredAt { get; set; } = DateTime.Now;
}
