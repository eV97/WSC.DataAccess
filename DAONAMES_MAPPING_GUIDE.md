# Hướng dẫn Ánh xạ DaoNames từ Project Sử dụng

## 📌 Tổng quan

Tài liệu này hướng dẫn cách **project sử dụng** (consuming project) khai báo `DaoNames` riêng và ánh xạ về WSC.DataAccess.

### Kiến trúc 3 tầng

```
┌────────────────────────────────────────────────────────────────┐
│ TẦNG 1: Project Sử dụng - Khai báo DaoNames                  │
├────────────────────────────────────────────────────────────────┤
│ MrFu.SmartCheck.Web.Models/Provider.cs                        │
│                                                                 │
│ public static class Provider                                  │
│ {                                                              │
│     public static readonly string DAO000 = "DAO000"; // Assets│
│     public static readonly string DAO001 = "DAO001"; // Cats  │
│     public static readonly string DAO002 = "DAO002"; // Locs  │
│     ...                                                        │
│ }                                                              │
└────────────────────────────────────────────────────────────────┘
                              ↓
┌────────────────────────────────────────────────────────────────┐
│ TẦNG 2: Program.cs - Ánh xạ DaoNames → File paths            │
├────────────────────────────────────────────────────────────────┤
│ services.AddWscDataAccess(connectionString, options =>        │
│ {                                                              │
│     options.ConfigureSqlMaps(provider =>                      │
│     {                                                          │
│         provider.AddFile(Provider.DAO000, "DAO/DAO000.xml");  │
│         provider.AddFile(Provider.DAO001, "DAO/DAO001.xml");  │
│         provider.AddFile(Provider.DAO002, "DAO/DAO002.xml");  │
│     });                                                        │
│ });                                                            │
└────────────────────────────────────────────────────────────────┘
                              ↓
┌────────────────────────────────────────────────────────────────┐
│ TẦNG 3: Service/Repository - Sử dụng DaoNames                │
├────────────────────────────────────────────────────────────────┤
│ // Cách 1: Service dùng 1 DAO                                 │
│ public class AssetService : ProviderBasedRepository<Asset>   │
│ {                                                              │
│     public AssetService(..., SqlMapProvider provider)         │
│         : base(..., provider, Provider.DAO000) // Assets      │
│     { }                                                        │
│ }                                                              │
│                                                                 │
│ // Cách 2: Service dùng NHIỀU DAOs                            │
│ public class ComplexService : MultiDaoProviderRepository      │
│ {                                                              │
│     private static readonly string[] DAO_NAMES = new[]        │
│     {                                                          │
│         Provider.DAO000, // Assets                            │
│         Provider.DAO001, // Categories                        │
│         Provider.DAO002  // Locations                         │
│     };                                                         │
│                                                                 │
│     public ComplexService(..., SqlMapProvider provider)       │
│         : base(..., provider, DAO_NAMES)                      │
│     { }                                                        │
│ }                                                              │
└────────────────────────────────────────────────────────────────┘
```

---

## 🔧 Bước 1: Khai báo Provider trong Project Sử dụng

### File: `Models/Provider.cs`

```csharp
namespace MrFu.SmartCheck.Web.Models;

/// <summary>
/// Định nghĩa các DAO names cho SmartCheck application
/// Pattern: DAO{Number} = Domain/Feature name
/// </summary>
public static class Provider
{
    // Asset Management
    public static readonly string DAO000 = "DAO000"; // Assets
    public static readonly string DAO001 = "DAO001"; // Categories
    public static readonly string DAO002 = "DAO002"; // Locations

    // Check Session Management
    public static readonly string DAO003 = "DAO003"; // CheckSessions
    public static readonly string DAO004 = "DAO004"; // CheckItems
    public static readonly string DAO005 = "DAO005"; // AssetHistory
    public static readonly string DAO006 = "DAO006"; // Alerts

    // System Management
    public static readonly string DAO014 = "DAO014"; // Check Session Assignment
    public static readonly string DAO015 = "DAO015"; // Role Management
    public static readonly string DAO016 = "DAO016"; // Menu Management
    public static readonly string DAO017 = "DAO017"; // Permission Management
    public static readonly string DAO018 = "DAO018"; // Department Management
    public static readonly string DAO019 = "DAO019"; // Asset Category Management
    public static readonly string DAO020 = "DAO020"; // Location Management

    // Reports
    public static readonly string DAO009 = "DAO009"; // Report: Báo cáo Tổng quan Phiên Kiểm kê
    public static readonly string DAO010 = "DAO010"; // Report: Báo cáo Chi tiết Tài sản
    public static readonly string DAO011 = "DAO011"; // Report: Báo cáo Chênh lệch
    public static readonly string DAO012 = "DAO012"; // Report: Báo cáo Lịch sử Kiểm kê
    public static readonly string DAO013 = "DAO013"; // Report: Báo cáo Dụng cụ Hộ lý

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
    /// <param name="baseDirectory">Thư mục gốc chứa DAO files (ví dụ: "DAO")</param>
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
    /// Lấy description của DAO từ comment/field name
    /// </summary>
    public static string GetDescription(string daoName)
    {
        // Mapping description cho từng DAO
        var descriptions = new Dictionary<string, string>
        {
            { DAO000, "Asset Management" },
            { DAO001, "Category Management" },
            { DAO002, "Location Management" },
            { DAO003, "Check Session Management" },
            { DAO004, "Check Item Management" },
            { DAO005, "Asset History Tracking" },
            { DAO006, "Alert Management" },
            { DAO009, "Report: Check Session Overview" },
            { DAO010, "Report: Asset Details" },
            { DAO011, "Report: Discrepancy Analysis" },
            { DAO012, "Report: Check History" },
            { DAO013, "Report: Medical Supplies" },
            { DAO014, "Check Session Assignment" },
            { DAO015, "Role Management" },
            { DAO016, "Menu Management" },
            { DAO017, "Permission Management" },
            { DAO018, "Department Management" },
            { DAO019, "Asset Category Management" },
            { DAO020, "Location Management (Extended)" }
        };

        return descriptions.TryGetValue(daoName, out var desc) ? desc : "Unknown DAO";
    }
}
```

### Lợi ích của pattern này:

✅ **IntelliSense support**: `Provider.DAO000` thay vì magic string `"DAO000"`
✅ **Type safety**: Compiler check, tránh typo
✅ **Centralized**: Tất cả DAO names ở 1 nơi
✅ **Self-documenting**: Comments giải thích từng DAO
✅ **Helper methods**: `GetAllDaoNames()`, `IsValidDaoName()`

---

## 🔧 Bước 2: Ánh xạ trong Program.cs/Startup.cs

### Cách 1: Ánh xạ từng file (Recommended)

```csharp
// File: Program.cs hoặc Startup.cs

using MrFu.SmartCheck.Web.Models;
using WSC.DataAccess.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Khai báo connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// ✅ ĐĂNG KÝ WSC.DataAccess với ánh xạ DaoNames
builder.Services.AddWscDataAccess(connectionString, options =>
{
    // Configure SQL Map Provider - Ánh xạ DaoNames → File paths
    options.ConfigureSqlMaps(provider =>
    {
        // Asset Management Domain
        provider.AddFile(Provider.DAO000, "DAO/DAO000.xml", "Asset Management");
        provider.AddFile(Provider.DAO001, "DAO/DAO001.xml", "Category Management");
        provider.AddFile(Provider.DAO002, "DAO/DAO002.xml", "Location Management");

        // Check Session Domain
        provider.AddFile(Provider.DAO003, "DAO/DAO003.xml", "Check Session Management");
        provider.AddFile(Provider.DAO004, "DAO/DAO004.xml", "Check Item Management");
        provider.AddFile(Provider.DAO005, "DAO/DAO005.xml", "Asset History");
        provider.AddFile(Provider.DAO006, "DAO/DAO006.xml", "Alert Management");

        // System Management Domain
        provider.AddFile(Provider.DAO014, "DAO/DAO014.xml", "Check Session Assignment");
        provider.AddFile(Provider.DAO015, "DAO/DAO015.xml", "Role Management");
        provider.AddFile(Provider.DAO016, "DAO/DAO016.xml", "Menu Management");
        provider.AddFile(Provider.DAO017, "DAO/DAO017.xml", "Permission Management");
        provider.AddFile(Provider.DAO018, "DAO/DAO018.xml", "Department Management");
        provider.AddFile(Provider.DAO019, "DAO/DAO019.xml", "Asset Category Management");
        provider.AddFile(Provider.DAO020, "DAO/DAO020.xml", "Location Management Extended");

        // Reports Domain
        provider.AddFile(Provider.DAO009, "DAO/DAO009.xml", "Report: Check Session Overview");
        provider.AddFile(Provider.DAO010, "DAO/DAO010.xml", "Report: Asset Details");
        provider.AddFile(Provider.DAO011, "DAO/DAO011.xml", "Report: Discrepancy");
        provider.AddFile(Provider.DAO012, "DAO/DAO012.xml", "Report: Check History");
        provider.AddFile(Provider.DAO013, "DAO/DAO013.xml", "Report: Medical Supplies");
    });
});

// Đăng ký Services/Repositories
builder.Services.AddScoped<AssetService>();
builder.Services.AddScoped<CheckSessionService>();
builder.Services.AddScoped<ReportService>();

var app = builder.Build();
```

### Cách 2: Ánh xạ tự động từ Provider helper

```csharp
// File: Program.cs

builder.Services.AddWscDataAccess(connectionString, options =>
{
    options.ConfigureSqlMaps(provider =>
    {
        // ✅ Tự động register tất cả DAOs từ Provider
        var daoNames = Provider.GetAllDaoNames();

        foreach (var daoName in daoNames)
        {
            var filePath = $"DAO/{daoName}.xml";
            var description = Provider.GetDescription(daoName);

            provider.AddFile(daoName, filePath, description);
        }
    });
});
```

### Cách 3: Ánh xạ với Multiple Connections

```csharp
// File: Program.cs

var mainConnection = builder.Configuration.GetConnectionString("MainDB");
var archiveConnection = builder.Configuration.GetConnectionString("ArchiveDB");
var reportConnection = builder.Configuration.GetConnectionString("ReportDB");

builder.Services.AddWscDataAccess(mainConnection, options =>
{
    // Đăng ký named connections
    options.AddConnection("MainDB", mainConnection);
    options.AddConnection("ArchiveDB", archiveConnection);
    options.AddConnection("ReportDB", reportConnection);

    options.ConfigureSqlMaps(provider =>
    {
        // Main database - Asset management
        provider.AddFile(Provider.DAO000, "DAO/DAO000.xml", "MainDB", "Assets");
        provider.AddFile(Provider.DAO001, "DAO/DAO001.xml", "MainDB", "Categories");
        provider.AddFile(Provider.DAO002, "DAO/DAO002.xml", "MainDB", "Locations");

        // Archive database - Historical data
        provider.AddFile(Provider.DAO005, "DAO/DAO005.xml", "ArchiveDB", "Asset History");

        // Report database - Analytics
        provider.AddFile(Provider.DAO009, "DAO/DAO009.xml", "ReportDB", "Reports");
        provider.AddFile(Provider.DAO010, "DAO/DAO010.xml", "ReportDB", "Reports");
        provider.AddFile(Provider.DAO011, "DAO/DAO011.xml", "ReportDB", "Reports");
    });
});
```

---

## 🔧 Bước 3: Sử dụng trong Service/Repository

### Pattern 1: Service sử dụng 1 DAO (Single Domain)

```csharp
// File: Services/AssetService.cs

using MrFu.SmartCheck.Web.Models;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Core;
using WSC.DataAccess.Repository;

namespace MrFu.SmartCheck.Web.Services;

public class AssetService : ProviderBasedRepository<Asset>
{
    // ✅ Khai báo DAO name sử dụng
    private const string DAO_NAME = Provider.DAO000; // Assets

    public AssetService(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider,
        ILogger<AssetService> logger)
        : base(sessionFactory, provider, DAO_NAME, logger: logger)
    {
    }

    // Business methods sử dụng statements từ DAO000.xml
    public async Task<IEnumerable<Asset>> GetAllAssetsAsync()
    {
        return await QueryListAsync("Asset.GetAll");
    }

    public async Task<Asset?> GetAssetByIdAsync(int id)
    {
        return await QuerySingleAsync("Asset.GetById", new { Id = id });
    }

    public async Task<int> CreateAssetAsync(Asset asset)
    {
        return await ExecuteAsync("Asset.Insert", asset);
    }

    public async Task<int> UpdateAssetAsync(Asset asset)
    {
        return await ExecuteAsync("Asset.Update", asset);
    }
}
```

### Pattern 2: Service sử dụng NHIỀU DAOs (Multiple Domains)

#### Option 2A: Dùng ScopedSqlMapRepository (Không dùng Provider)

```csharp
// File: Services/CheckSessionService.cs

using WSC.DataAccess.Core;
using WSC.DataAccess.Repository;

public class CheckSessionService : ScopedSqlMapRepository<dynamic>
{
    // ✅ Khai báo nhiều DAO files
    private static readonly string[] SQL_MAP_FILES = new[]
    {
        "DAO/DAO003.xml",  // CheckSessions
        "DAO/DAO004.xml",  // CheckItems
        "DAO/DAO000.xml",  // Assets
        "DAO/DAO006.xml"   // Alerts
    };

    public CheckSessionService(
        IDbSessionFactory sessionFactory,
        ILogger<CheckSessionService> logger)
        : base(sessionFactory, SQL_MAP_FILES, logger: logger)
    {
    }

    // Method sử dụng statements từ NHIỀU DAO files
    public async Task<CheckSession?> GetCheckSessionAsync(int id)
    {
        // Statement từ DAO003.xml
        return await QuerySingleAsync<CheckSession>(
            "CheckSession.GetById", new { Id = id });
    }

    public async Task<IEnumerable<CheckItem>> GetCheckItemsAsync(int sessionId)
    {
        // Statement từ DAO004.xml
        return await QueryListAsync<CheckItem>(
            "CheckItem.GetBySession", new { SessionId = sessionId });
    }

    public async Task<Asset?> GetAssetDetailsAsync(int assetId)
    {
        // Statement từ DAO000.xml
        return await QuerySingleAsync<Asset>(
            "Asset.GetById", new { Id = assetId });
    }

    public async Task<int> CreateAlertAsync(Alert alert)
    {
        // Statement từ DAO006.xml
        return await ExecuteAsync("Alert.Insert", alert);
    }
}
```

#### Option 2B: Dùng MultiDaoProviderRepository (Recommended - Kết hợp Provider)

```csharp
// File: Services/CheckSessionService.cs

using MrFu.SmartCheck.Web.Models;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Core;
using WSC.DataAccess.Repository;

public class CheckSessionService : MultiDaoProviderRepository<dynamic>
{
    // ✅ Khai báo nhiều DAO names từ Provider
    private static readonly string[] DAO_NAMES = new[]
    {
        Provider.DAO003,  // CheckSessions
        Provider.DAO004,  // CheckItems
        Provider.DAO000,  // Assets
        Provider.DAO006   // Alerts
    };

    public CheckSessionService(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider,
        ILogger<CheckSessionService> logger)
        : base(sessionFactory, provider, DAO_NAMES, logger: logger)
    {
        // Provider tự động resolve:
        // Provider.DAO003 → "DAO/DAO003.xml"
        // Provider.DAO004 → "DAO/DAO004.xml"
        // Provider.DAO000 → "DAO/DAO000.xml"
        // Provider.DAO006 → "DAO/DAO006.xml"
    }

    // Same methods as Option 2A
    public async Task<CheckSession?> GetCheckSessionAsync(int id)
    {
        return await QuerySingleAsync<CheckSession>(
            "CheckSession.GetById", new { Id = id });
    }

    // ... other methods
}
```

### Pattern 3: Report Service sử dụng NHIỀU DAOs (Reports)

```csharp
// File: Services/ReportService.cs

using MrFu.SmartCheck.Web.Models;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Core;
using WSC.DataAccess.Repository;

public class ReportService : MultiDaoProviderRepository<dynamic>
{
    // ✅ Khai báo tất cả Report DAOs
    private static readonly string[] REPORT_DAOS = new[]
    {
        Provider.DAO009,  // Report: Tổng quan Phiên Kiểm kê
        Provider.DAO010,  // Report: Chi tiết Tài sản
        Provider.DAO011,  // Report: Chênh lệch
        Provider.DAO012,  // Report: Lịch sử Kiểm kê
        Provider.DAO013   // Report: Dụng cụ Hộ lý
    };

    public ReportService(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider,
        ILogger<ReportService> logger)
        : base(sessionFactory, provider, REPORT_DAOS, logger: logger)
    {
    }

    public async Task<IEnumerable<CheckSessionOverviewReport>> GetCheckSessionOverviewAsync(
        DateTime fromDate, DateTime toDate)
    {
        // Statement từ DAO009.xml
        return await QueryListAsync<CheckSessionOverviewReport>(
            "Report.CheckSessionOverview",
            new { FromDate = fromDate, ToDate = toDate });
    }

    public async Task<IEnumerable<AssetDetailReport>> GetAssetDetailsReportAsync(
        int? categoryId = null)
    {
        // Statement từ DAO010.xml
        return await QueryListAsync<AssetDetailReport>(
            "Report.AssetDetails",
            new { CategoryId = categoryId });
    }

    public async Task<IEnumerable<DiscrepancyReport>> GetDiscrepancyReportAsync(
        int checkSessionId)
    {
        // Statement từ DAO011.xml
        return await QueryListAsync<DiscrepancyReport>(
            "Report.Discrepancy",
            new { CheckSessionId = checkSessionId });
    }
}
```

---

## 📂 Cấu trúc thư mục Project

```
MrFu.SmartCheck.Web/
├── Models/
│   └── Provider.cs                    ← Khai báo DaoNames
│
├── Services/                          ← Business logic services
│   ├── AssetService.cs               ← Dùng 1 DAO (DAO000)
│   ├── CategoryService.cs            ← Dùng 1 DAO (DAO001)
│   ├── CheckSessionService.cs        ← Dùng NHIỀU DAOs (003, 004, 000, 006)
│   ├── ReportService.cs              ← Dùng NHIỀU DAOs (009-013)
│   └── SystemService.cs              ← Dùng NHIỀU DAOs (015-020)
│
├── DAO/                               ← SQL Map XML files
│   ├── DAO000.xml                    ← Asset statements
│   ├── DAO001.xml                    ← Category statements
│   ├── DAO002.xml                    ← Location statements
│   ├── DAO003.xml                    ← CheckSession statements
│   ├── DAO004.xml                    ← CheckItem statements
│   ├── DAO005.xml                    ← AssetHistory statements
│   ├── DAO006.xml                    ← Alert statements
│   ├── DAO009.xml                    ← Report: Overview
│   ├── DAO010.xml                    ← Report: Asset Details
│   ├── DAO011.xml                    ← Report: Discrepancy
│   ├── DAO012.xml                    ← Report: History
│   ├── DAO013.xml                    ← Report: Medical Supplies
│   ├── DAO014.xml                    ← Check Session Assignment
│   ├── DAO015.xml                    ← Role Management
│   ├── DAO016.xml                    ← Menu Management
│   ├── DAO017.xml                    ← Permission Management
│   ├── DAO018.xml                    ← Department Management
│   ├── DAO019.xml                    ← Asset Category Management
│   └── DAO020.xml                    ← Location Management Extended
│
├── Program.cs                         ← Configure SqlMapProvider
└── appsettings.json                   ← Connection strings
```

---

## 🔍 So sánh các Patterns

| Pattern | Use Case | Base Class | Pros | Cons |
|---------|----------|------------|------|------|
| **Pattern 1**: Single DAO | Service chỉ cần 1 domain | `ProviderBasedRepository` | ✅ Simple<br>✅ IntelliSense<br>✅ Provider pattern | ❌ Chỉ 1 DAO |
| **Pattern 2A**: Multiple DAOs (No Provider) | Service cần nhiều domains | `ScopedSqlMapRepository` | ✅ Load nhiều files<br>✅ Đơn giản | ❌ Hardcode paths<br>❌ Không dùng Provider |
| **Pattern 2B**: Multiple DAOs (Provider) | Service cần nhiều domains | `MultiDaoProviderRepository` | ✅ Load nhiều files<br>✅ Provider pattern<br>✅ IntelliSense<br>✅ Centralized config | ❌ Cần base class mới |

**Khuyến nghị**: Dùng **Pattern 2B (MultiDaoProviderRepository)** cho services phức tạp.

---

## ✅ Best Practices

### 1. Naming Convention

```csharp
// ✅ GOOD: Clear, descriptive constants
public static class Provider
{
    public static readonly string DAO000 = "DAO000"; // Assets
    public static readonly string DAO001 = "DAO001"; // Categories
}

// ❌ BAD: Magic strings
var service = new AssetService(..., provider, "DAO000");
```

### 2. Grouping DAOs by Domain

```csharp
// ✅ GOOD: Group related DAOs in service
private static readonly string[] ASSET_DOMAIN = new[]
{
    Provider.DAO000,  // Assets
    Provider.DAO001,  // Categories
    Provider.DAO002   // Locations
};

// ❌ BAD: Unrelated DAOs
private static readonly string[] MIXED_DAOS = new[]
{
    Provider.DAO000,  // Assets
    Provider.DAO009,  // Reports
    Provider.DAO015   // Roles - not related!
};
```

### 3. Error Handling

```csharp
public class AssetService : ProviderBasedRepository<Asset>
{
    public async Task<Asset?> GetAssetAsync(int id)
    {
        try
        {
            return await QuerySingleAsync("Asset.GetById", new { Id = id });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            // DAO name not registered in provider
            _logger.LogError(ex, "DAO configuration error");
            throw new InvalidOperationException(
                $"DAO {Provider.DAO000} is not configured. Check Program.cs ConfigureSqlMaps()");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get asset {AssetId}", id);
            throw;
        }
    }
}
```

### 4. Validation

```csharp
// Trong Program.cs - Validate tất cả DAOs đã được register
var provider = app.Services.GetRequiredService<SqlMapProvider>();
var missingDaos = Provider.GetAllDaoNames()
    .Where(dao => !provider.HasFile(dao))
    .ToArray();

if (missingDaos.Any())
{
    throw new InvalidOperationException(
        $"Missing DAO registrations: {string.Join(", ", missingDaos)}. " +
        $"Please register them in ConfigureSqlMaps()");
}
```

---

## 🧪 Testing

### Unit Test Example

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Core;
using Xunit;

public class AssetServiceTests
{
    [Fact]
    public async Task GetAssetById_ShouldReturnAsset()
    {
        // Arrange
        var mockSessionFactory = new Mock<IDbSessionFactory>();
        var provider = new SqlMapProvider();
        provider.AddFile(Provider.DAO000, "DAO/DAO000.xml");

        var service = new AssetService(
            mockSessionFactory.Object,
            provider,
            NullLogger<AssetService>.Instance);

        // Act
        var result = await service.GetAssetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Constructor_WithMissingDaoRegistration_ShouldThrow()
    {
        // Arrange
        var mockSessionFactory = new Mock<IDbSessionFactory>();
        var provider = new SqlMapProvider(); // Empty provider - no DAOs registered

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new AssetService(
                mockSessionFactory.Object,
                provider,
                NullLogger<AssetService>.Instance));

        Assert.Contains("DAO000", ex.Message);
        Assert.Contains("not found in provider", ex.Message);
    }
}
```

---

## 🚀 Migration Path (từ cũ sang mới)

### Old Way (Hardcoded paths)

```csharp
// ❌ OLD: Hardcode file paths
public class AssetService : ScopedSqlMapRepository<Asset>
{
    public AssetService(IDbSessionFactory factory)
        : base(factory, "SqlMaps/DAO005.xml")
    {
    }
}
```

### New Way (Provider pattern)

```csharp
// ✅ NEW: Dùng Provider pattern
public class AssetService : ProviderBasedRepository<Asset>
{
    private const string DAO_NAME = Provider.DAO000;

    public AssetService(
        IDbSessionFactory factory,
        SqlMapProvider provider)
        : base(factory, provider, DAO_NAME)
    {
    }
}

// Program.cs
services.AddWscDataAccess(connectionString, options =>
{
    options.ConfigureSqlMaps(provider =>
    {
        provider.AddFile(Provider.DAO000, "DAO/DAO000.xml");
    });
});
```

---

## 📚 Related Documentation

- [PROVIDER_PATTERN_GUIDE.md](PROVIDER_PATTERN_GUIDE.md) - Provider pattern details
- [MULTI_CONNECTION_GUIDE.md](MULTI_CONNECTION_GUIDE.md) - Multiple database connections
- [SQLMAP_CONSTANTS_GUIDE.md](SQLMAP_CONSTANTS_GUIDE.md) - SQL Map constants usage

---

## ❓ FAQ

### Q1: Có thể dùng tên DAO khác "DAO000" không?

**A**: Có! Provider key có thể là BẤT KỲ string nào:

```csharp
// Dùng tên descriptive
public static class Provider
{
    public static readonly string ASSETS = "Assets";
    public static readonly string USERS = "Users";
}

// Program.cs
provider.AddFile(Provider.ASSETS, "DAO/Assets.xml");
provider.AddFile(Provider.USERS, "DAO/Users.xml");
```

### Q2: Service có thể dùng cả Provider pattern VÀ hardcoded paths không?

**A**: Nên chọn 1 trong 2 patterns để consistency:
- **Provider pattern**: Centralized config, recommended
- **Hardcoded paths**: Quick prototyping, not recommended for production

### Q3: Làm sao để 1 service dùng DAOs từ nhiều connections khác nhau?

**A**: Xem [MULTI_CONNECTION_GUIDE.md](MULTI_CONNECTION_GUIDE.md) - Pattern "Cross-Connection Services"

---

## 📞 Support

Có câu hỏi? Tạo issue tại: [GitHub Issues](https://github.com/eV97/WSC.DataAccess/issues)
