namespace WSC.DataAccess.RealDB.Test.Models;

/// <summary>
/// Model for Application table in LP_ApplicationSystem
/// Đây là ví dụ - bạn cần adjust theo cấu trúc table thực tế của bạn
/// </summary>
public class Application
{
    public int Id { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Version { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Model for System Configuration
/// </summary>
public class SystemConfig
{
    public int Id { get; set; }
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
