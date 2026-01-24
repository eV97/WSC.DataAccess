# Multiple Connections với cùng DaoNames

## 🎯 Use Case

**Tình huống**: Cùng 1 DAO file (ví dụ: `DAO000.xml`) nhưng trong service:
- 1 số hàm kết nối tới **Main Database** (hệ thống chính)
- 1 số hàm khác kết nối tới **Report Database** (read-only replica)

## 🏗️ Kiến trúc

```
┌────────────────────────────────────────────────────────────────┐
│ Service (AssetService)                                         │
├────────────────────────────────────────────────────────────────┤
│                                                                 │
│ GetAssetById()         → MainDB    (Read/Write)               │
│ CreateAsset()          → MainDB    (Read/Write)               │
│ UpdateAsset()          → MainDB    (Read/Write)               │
│                                                                 │
│ GetAssetReport()       → ReportDB  (Read-Only)                │
│ GetAssetSummary()      → ReportDB  (Read-Only)                │
│                                                                 │
└────────────────────────────────────────────────────────────────┘
                    ↓                        ↓
         ┌──────────────────┐      ┌──────────────────┐
         │ Main Database    │      │ Report Database  │
         │ (Read/Write)     │      │ (Read-Only)      │
         │                  │      │ (Replica/ETL)    │
         │ DAO000.xml       │      │ DAO000.xml       │
         └──────────────────┘      └──────────────────┘
```

## 📋 Cách 1: Method-level Connection Selection (Recommended)

### Bước 1: Configure Multiple Connections trong Program.cs

```csharp
using MrFu.SmartCheck.Web.Models;

var builder = WebApplication.CreateBuilder(args);

// ✅ Lấy connection strings từ appsettings.json
var mainConnection = builder.Configuration.GetConnectionString("MainDB");
var reportConnection = builder.Configuration.GetConnectionString("ReportDB");

builder.Services.AddWscDataAccess(mainConnection, options =>
{
    // ✅ Đăng ký named connections
    options.AddConnection("MainDB", mainConnection);
    options.AddConnection("ReportDB", reportConnection);

    // ✅ Đăng ký DAO files cho từng connection
    options.ConfigureSqlMaps(provider =>
    {
        // DAO000 cho MainDB (hệ thống chính)
        provider.AddFile(Provider.DAO000, "DAO/DAO000.xml", "MainDB", "Assets - Main");

        // DAO000 cho ReportDB (report database)
        provider.AddFile(Provider.DAO000, "DAO/DAO000.xml", "ReportDB", "Assets - Report");

        // Các DAOs khác
        provider.AddFile(Provider.DAO001, "DAO/DAO001.xml", "MainDB", "Categories");
        provider.AddFile(Provider.DAO001, "DAO/DAO001.xml", "ReportDB", "Categories - Report");
    });
});
```

### Bước 2: appsettings.json

```json
{
  "ConnectionStrings": {
    "MainDB": "Server=main-server;Database=SmartCheck;User Id=sa;Password=***;TrustServerCertificate=True",
    "ReportDB": "Server=report-server;Database=SmartCheck_Report;User Id=reader;Password=***;TrustServerCertificate=True"
  }
}
```

### Bước 3: Service sử dụng Multiple Connections

```csharp
using WSC.DataAccess.Repository;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Core;
using MrFu.SmartCheck.Web.Models;

namespace MrFu.SmartCheck.Web.Services;

public class AssetService : ProviderBasedRepository<Asset>
{
    private const string DAO_NAME = Provider.DAO000;

    // ✅ Connection names
    private const string MAIN_CONNECTION = "MainDB";      // Read/Write
    private const string REPORT_CONNECTION = "ReportDB";  // Read-Only

    public AssetService(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider,
        ILogger<AssetService> logger)
        : base(sessionFactory, provider, DAO_NAME, logger: logger)
    {
        // Base constructor load DAO000.xml cho default connection
    }

    #region Main Database Operations (Read/Write)

    /// <summary>
    /// Lấy asset từ Main Database
    /// </summary>
    public async Task<Asset?> GetAssetByIdAsync(int id)
    {
        Logger?.LogInformation("Getting asset {AssetId} from MainDB", id);

        // ✅ Sử dụng MAIN_CONNECTION
        return await QuerySingleAsync(
            "Asset.GetById",
            new { Id = id },
            MAIN_CONNECTION);  // ← Chỉ định connection
    }

    /// <summary>
    /// Tạo asset mới trong Main Database
    /// </summary>
    public async Task<int> CreateAssetAsync(Asset asset)
    {
        Logger?.LogInformation("Creating asset in MainDB");

        // ✅ Sử dụng MAIN_CONNECTION (Read/Write)
        return await ExecuteAsync(
            "Asset.Insert",
            asset,
            MAIN_CONNECTION);  // ← Chỉ định connection
    }

    /// <summary>
    /// Cập nhật asset trong Main Database
    /// </summary>
    public async Task<int> UpdateAssetAsync(Asset asset)
    {
        Logger?.LogInformation("Updating asset {AssetId} in MainDB", asset.Id);

        // ✅ Sử dụng MAIN_CONNECTION (Read/Write)
        return await ExecuteAsync(
            "Asset.Update",
            asset,
            MAIN_CONNECTION);  // ← Chỉ định connection
    }

    #endregion

    #region Report Database Operations (Read-Only)

    /// <summary>
    /// Lấy danh sách assets từ Report Database
    /// Report DB có thể là read replica hoặc ETL database
    /// </summary>
    public async Task<IEnumerable<Asset>> GetAssetsForReportAsync()
    {
        Logger?.LogInformation("Getting assets from ReportDB");

        // ✅ Sử dụng REPORT_CONNECTION (Read-Only)
        return await QueryListAsync(
            "Asset.GetAll",
            null,
            REPORT_CONNECTION);  // ← Chỉ định connection khác
    }

    /// <summary>
    /// Lấy asset summary từ Report Database
    /// </summary>
    public async Task<AssetSummary?> GetAssetSummaryAsync()
    {
        Logger?.LogInformation("Getting asset summary from ReportDB");

        // ✅ Sử dụng REPORT_CONNECTION
        return await QuerySingleAsync<AssetSummary>(
            "Asset.GetSummary",
            null,
            REPORT_CONNECTION);  // ← Chỉ định connection khác
    }

    /// <summary>
    /// Lấy asset statistics từ Report Database
    /// </summary>
    public async Task<IEnumerable<AssetStatistic>> GetAssetStatisticsAsync(
        DateTime fromDate, DateTime toDate)
    {
        Logger?.LogInformation("Getting asset statistics from ReportDB: {From} - {To}",
            fromDate, toDate);

        // ✅ Sử dụng REPORT_CONNECTION
        return await QueryListAsync<AssetStatistic>(
            "Asset.GetStatistics",
            new { FromDate = fromDate, ToDate = toDate },
            REPORT_CONNECTION);  // ← Chỉ định connection khác
    }

    #endregion

    #region Cross-Connection Operations

    /// <summary>
    /// Lấy asset từ Main DB, lấy history từ Report DB
    /// </summary>
    public async Task<AssetDetailReport> GetAssetDetailReportAsync(int assetId)
    {
        Logger?.LogInformation("Getting asset detail report for {AssetId}", assetId);

        // 1. Lấy asset từ MainDB
        var asset = await QuerySingleAsync(
            "Asset.GetById",
            new { Id = assetId },
            MAIN_CONNECTION);  // ← Main DB

        if (asset == null)
        {
            throw new InvalidOperationException($"Asset {assetId} not found");
        }

        // 2. Lấy history từ ReportDB
        var history = await QueryListAsync<AssetHistory>(
            "Asset.GetHistory",
            new { AssetId = assetId },
            REPORT_CONNECTION);  // ← Report DB

        return new AssetDetailReport
        {
            Asset = asset,
            History = history.ToList()
        };
    }

    #endregion
}

#region Supporting Models

public class AssetSummary
{
    public int TotalAssets { get; set; }
    public int ActiveAssets { get; set; }
    public int InactiveAssets { get; set; }
    public decimal TotalValue { get; set; }
}

public class AssetStatistic
{
    public DateTime Date { get; set; }
    public int AssetCount { get; set; }
    public decimal TotalValue { get; set; }
}

public class AssetHistory
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTime ActionDate { get; set; }
    public string ActionBy { get; set; } = string.Empty;
}

public class AssetDetailReport
{
    public Asset Asset { get; set; } = null!;
    public List<AssetHistory> History { get; set; } = new();
}

#endregion
```

## 📋 Cách 2: Repository-level Connection (Specialized Repositories)

Tạo 2 repositories riêng biệt cho 2 mục đích:

### Repository cho Main Database

```csharp
public class AssetRepository : ProviderBasedRepository<Asset>
{
    private const string DAO_NAME = Provider.DAO000;
    private const string CONNECTION_NAME = "MainDB";

    public AssetRepository(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider,
        ILogger<AssetRepository> logger)
        : base(sessionFactory, provider, DAO_NAME, CONNECTION_NAME, logger: logger)
    {
        // Luôn sử dụng MainDB connection
    }

    // Tất cả methods mặc định dùng MainDB
    public async Task<Asset?> GetByIdAsync(int id)
    {
        return await QuerySingleAsync("Asset.GetById", new { Id = id });
    }

    public async Task<int> CreateAsync(Asset asset)
    {
        return await ExecuteAsync("Asset.Insert", asset);
    }
}
```

### Repository cho Report Database

```csharp
public class AssetReportRepository : ProviderBasedRepository<Asset>
{
    private const string DAO_NAME = Provider.DAO000;
    private const string CONNECTION_NAME = "ReportDB";

    public AssetReportRepository(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider,
        ILogger<AssetReportRepository> logger)
        : base(sessionFactory, provider, DAO_NAME, CONNECTION_NAME, logger: logger)
    {
        // Luôn sử dụng ReportDB connection
    }

    // Tất cả methods mặc định dùng ReportDB
    public async Task<IEnumerable<Asset>> GetForReportAsync()
    {
        return await QueryListAsync("Asset.GetAll");
    }

    public async Task<AssetSummary?> GetSummaryAsync()
    {
        return await QuerySingleAsync<AssetSummary>("Asset.GetSummary");
    }
}
```

### Service sử dụng cả 2 Repositories

```csharp
public class AssetService
{
    private readonly AssetRepository _mainRepo;        // MainDB
    private readonly AssetReportRepository _reportRepo; // ReportDB
    private readonly ILogger<AssetService> _logger;

    public AssetService(
        AssetRepository mainRepo,
        AssetReportRepository reportRepo,
        ILogger<AssetService> logger)
    {
        _mainRepo = mainRepo;
        _reportRepo = reportRepo;
        _logger = logger;
    }

    // Hàm cho hệ thống - dùng MainDB
    public async Task<Asset?> GetAssetAsync(int id)
    {
        return await _mainRepo.GetByIdAsync(id);
    }

    public async Task<int> CreateAssetAsync(Asset asset)
    {
        return await _mainRepo.CreateAsync(asset);
    }

    // Hàm cho report - dùng ReportDB
    public async Task<IEnumerable<Asset>> GetAssetsForReportAsync()
    {
        return await _reportRepo.GetForReportAsync();
    }

    public async Task<AssetSummary?> GetAssetSummaryAsync()
    {
        return await _reportRepo.GetSummaryAsync();
    }
}
```

### Đăng ký trong Program.cs

```csharp
// Đăng ký cả 2 repositories
builder.Services.AddScoped<AssetRepository>();        // MainDB
builder.Services.AddScoped<AssetReportRepository>();  // ReportDB
builder.Services.AddScoped<AssetService>();           // Service dùng cả 2
```

## 📋 Cách 3: MultiDaoProviderRepository với Multiple Connections

Service dùng nhiều DAOs và nhiều connections:

```csharp
public class ComplexReportService : MultiDaoProviderRepository<dynamic>
{
    private static readonly string[] DAO_NAMES = new[]
    {
        Provider.DAO000,  // Assets
        Provider.DAO001,  // Categories
        Provider.DAO003   // CheckSessions
    };

    private const string MAIN_CONNECTION = "MainDB";
    private const string REPORT_CONNECTION = "ReportDB";

    public ComplexReportService(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider,
        ILogger<ComplexReportService> logger)
        : base(sessionFactory, provider, DAO_NAMES, logger: logger)
    {
    }

    /// <summary>
    /// Lấy assets từ Main DB
    /// </summary>
    public async Task<IEnumerable<Asset>> GetAssetsAsync()
    {
        return await QueryListAsync<Asset>(
            "Asset.GetAll",
            null,
            MAIN_CONNECTION);  // ← MainDB
    }

    /// <summary>
    /// Lấy asset statistics từ Report DB
    /// </summary>
    public async Task<IEnumerable<AssetStatistic>> GetStatisticsAsync()
    {
        return await QueryListAsync<AssetStatistic>(
            "Asset.GetStatistics",
            null,
            REPORT_CONNECTION);  // ← ReportDB
    }

    /// <summary>
    /// Cross-database report: Assets từ MainDB + Statistics từ ReportDB
    /// </summary>
    public async Task<AssetReport> GetAssetReportAsync()
    {
        // 1. Lấy assets từ MainDB
        var assets = await QueryListAsync<Asset>(
            "Asset.GetAll",
            null,
            MAIN_CONNECTION);

        // 2. Lấy statistics từ ReportDB
        var statistics = await QueryListAsync<AssetStatistic>(
            "Asset.GetStatistics",
            null,
            REPORT_CONNECTION);

        return new AssetReport
        {
            Assets = assets.ToList(),
            Statistics = statistics.ToList()
        };
    }
}

public class AssetReport
{
    public List<Asset> Assets { get; set; } = new();
    public List<AssetStatistic> Statistics { get; set; } = new();
}
```

## 🔍 So sánh 3 Cách

| Cách | Pros | Cons | Use Case |
|------|------|------|----------|
| **Cách 1: Method-level** | ✅ Linh hoạt<br>✅ 1 service duy nhất<br>✅ Dễ control | ❌ Phải chỉ định connection mỗi lần | Service cần cả read/write và report |
| **Cách 2: Repository-level** | ✅ Separation of concerns<br>✅ Auto connection<br>✅ Testable | ❌ 2 repositories riêng<br>❌ Nhiều code | Clear separation: Main vs Report |
| **Cách 3: Multi-DAO Multi-Connection** | ✅ Cross-database queries<br>✅ Powerful | ❌ Phức tạp hơn | Report service phức tạp |

## 💡 Best Practices

### 1. Naming Convention

```csharp
// ✅ GOOD: Clear connection names
private const string MAIN_CONNECTION = "MainDB";      // Read/Write
private const string REPORT_CONNECTION = "ReportDB";  // Read-Only
private const string ARCHIVE_CONNECTION = "ArchiveDB"; // Historical data

// ❌ BAD: Unclear names
private const string CONN1 = "Connection_1";
private const string CONN2 = "Connection_2";
```

### 2. Connection Purpose Documentation

```csharp
/// <summary>
/// Lấy asset từ Main Database (Read/Write)
/// </summary>
public async Task<Asset?> GetAssetAsync(int id)
{
    return await QuerySingleAsync("Asset.GetById", new { Id = id }, MAIN_CONNECTION);
}

/// <summary>
/// Lấy asset từ Report Database (Read-Only Replica)
/// Report DB được sync từ Main DB mỗi 5 phút
/// </summary>
public async Task<Asset?> GetAssetForReportAsync(int id)
{
    return await QuerySingleAsync("Asset.GetById", new { Id = id }, REPORT_CONNECTION);
}
```

### 3. Error Handling

```csharp
public async Task<Asset?> GetAssetAsync(int id)
{
    try
    {
        return await QuerySingleAsync("Asset.GetById", new { Id = id }, MAIN_CONNECTION);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get asset {AssetId} from MainDB", id);

        // Fallback to ReportDB if MainDB fails
        _logger.LogWarning("Falling back to ReportDB");
        return await QuerySingleAsync("Asset.GetById", new { Id = id }, REPORT_CONNECTION);
    }
}
```

### 4. Configuration Validation

```csharp
// Program.cs - Validate connections exist
var provider = app.Services.GetRequiredService<SqlMapProvider>();
var connections = new[] { "MainDB", "ReportDB" };

foreach (var conn in connections)
{
    if (!provider.GetAllConnectionNames().Contains(conn))
    {
        throw new InvalidOperationException($"Connection '{conn}' not configured");
    }
}
```

## 🎯 Use Cases

### Use Case 1: Read/Write Separation

```
MainDB (Primary)     →  Tất cả CRUD operations
ReportDB (Replica)   →  Tất cả read-only reports
```

### Use Case 2: Multi-Tenant

```
Tenant1DB  →  provider.AddFile(Provider.DAO000, "DAO/DAO000.xml", "Tenant1DB")
Tenant2DB  →  provider.AddFile(Provider.DAO000, "DAO/DAO000.xml", "Tenant2DB")
Tenant3DB  →  provider.AddFile(Provider.DAO000, "DAO/DAO000.xml", "Tenant3DB")
```

### Use Case 3: Archive/Historical Data

```
MainDB     →  Current data (last 6 months)
ArchiveDB  →  Historical data (older than 6 months)
```

## 📚 Related Documentation

- [MULTI_CONNECTION_GUIDE.md](MULTI_CONNECTION_GUIDE.md) - Multiple connections guide
- [DAONAMES_MAPPING_GUIDE.md](DAONAMES_MAPPING_GUIDE.md) - DaoNames mapping guide

## ✅ Summary

✅ **Cùng 1 DAO file**, **nhiều connections** → Fully supported!
✅ **Method-level selection** → Flexible, recommended
✅ **Repository-level separation** → Clean architecture
✅ **Cross-database queries** → Advanced scenarios

**Chọn cách phù hợp với use case của bạn! 🚀**
