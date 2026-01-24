namespace WSC.DataAccess.Sample.Models;

/// <summary>
/// Định nghĩa các DAO names cho Sample application
/// Pattern giống MrFu.SmartCheck.Web - Project sử dụng tự khai báo DaoNames
/// </summary>
public static class Provider
{
    // System & Configuration
    public static readonly string DAO000 = "DAO000"; // System & Configuration

    // Core Entities
    public static readonly string DAO001 = "DAO001"; // User Management
    public static readonly string DAO002 = "DAO002"; // Product Management
    public static readonly string DAO003 = "DAO003"; // Order Management
    public static readonly string DAO004 = "DAO004"; // Category Management

    // Analytics & Reports
    public static readonly string DAO005 = "DAO005"; // Reports & Analytics

    /// <summary>
    /// Lấy tất cả DAO names đã định nghĩa
    /// </summary>
    public static string[] GetAllDaoNames()
    {
        return typeof(Provider)
            .GetFields()
            .Where(f => f.IsStatic && f.IsInitOnly && f.Name.StartsWith("DAO"))
            .Select(f => f.GetValue(null)?.ToString())
            .Where(v => v != null)
            .ToArray()!;
    }

    /// <summary>
    /// Chuyển đổi DAO names thành file paths
    /// </summary>
    /// <param name="baseDirectory">Thư mục gốc chứa DAO files (ví dụ: "SqlMaps")</param>
    public static string[] GetDaoFiles(string baseDirectory)
    {
        return GetAllDaoNames()
            .Select(dao => Path.Combine(baseDirectory, $"{dao}.xml"))
            .ToArray();
    }

    /// <summary>
    /// Kiểm tra DAO name có hợp lệ không
    /// </summary>
    public static bool IsValidDaoName(string daoName)
    {
        return GetAllDaoNames().Contains(daoName);
    }

    /// <summary>
    /// Lấy description của DAO
    /// </summary>
    public static string GetDescription(string daoName)
    {
        var descriptions = new Dictionary<string, string>
        {
            { DAO000, "System & Configuration" },
            { DAO001, "User Management" },
            { DAO002, "Product Management" },
            { DAO003, "Order Management" },
            { DAO004, "Category Management" },
            { DAO005, "Reports & Analytics" }
        };

        return descriptions.TryGetValue(daoName, out var desc) ? desc : "Unknown DAO";
    }
}
