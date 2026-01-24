# ISql Pattern Guide - iBatis.NET Style API

## 🎯 Tổng quan

**ISql pattern** cung cấp API kiểu iBatis.NET - trực tiếp làm việc với connection và statement IDs, thay vì thông qua Repository pattern.

### Ưu điểm

✅ **Đơn giản**: Không cần tạo repository class cho mỗi entity
✅ **Linh hoạt**: Dễ dàng switch DAO và connection trong cùng 1 service
✅ **Quen thuộc**: API giống iBatis.NET, dễ migration
✅ **Tích hợp tốt**: Kết hợp với Provider pattern (DaoNames mapping)

---

## 🚀 Quick Start

### Bước 1: Setup trong Program.cs

```csharp
using MrFu.SmartCheck.Web.Models;

var builder = WebApplication.CreateBuilder(args);

// Connection strings
var mainConnection = builder.Configuration.GetConnectionString("MainDB");
var reportConnection = builder.Configuration.GetConnectionString("ReportDB");

// ✅ Register WSC.DataAccess với ISql
builder.Services.AddWscDataAccess(mainConnection, options =>
{
    // Named connections (optional)
    options.AddConnection("MainDB", mainConnection);
    options.AddConnection("ReportDB", reportConnection);

    // Map DAOs
    options.ConfigureSqlMaps(provider =>
    {
        // Main database
        provider.AddFile(Provider.DAO000, "DAO/DAO000.xml", "MainDB", "Assets");
        provider.AddFile(Provider.DAO001, "DAO/DAO001.xml", "MainDB", "Categories");

        // Report database
        provider.AddFile(Provider.DAO000, "DAO/DAO000.xml", "ReportDB", "Assets - Report");
    });
});

// ISql is automatically registered as Scoped service
// No need to manually register
```

### Bước 2: Inject ISql vào Service

```csharp
using WSC.DataAccess.Core;
using WSC.DataAccess.Extensions; // ← Extension methods

namespace MrFu.SmartCheck.Web.Services;

public class AssetService
{
    private readonly ISql _sql;
    private readonly ILogger<AssetService> _logger;

    public AssetService(ISql sql, ILogger<AssetService> logger)
    {
        _sql = sql;
        _logger = logger;
    }

    // Methods here...
}
```

### Bước 3: Sử dụng ISql

```csharp
public async Task<List<AssetDto>> GetAllAssetsAsync()
{
    try
    {
        // 1. Set DAO context
        _sql.GetDAO(Provider.DAO000);

        // 2. Create connection
        using var connection = _sql.CreateConnection();

        // 3. Execute statement
        var assets = await connection.StatementExecuteQueryAsync<AssetDto>("GetAllAssets");

        return assets.ToList();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving assets");
        throw;
    }
}
```

---

## 📋 ISql API Reference

### ISql Interface

```csharp
public interface ISql
{
    // Create connection
    ISqlConnection CreateConnection();
    ISqlConnection CreateConnection(string connectionName);

    // Switch DAO context
    void GetDAO(string daoName);
    void GetDAO(string daoName, string connectionName);

    // Current context
    string? CurrentDao { get; }
    string CurrentConnection { get; }
}
```

### ISqlConnection Extension Methods

```csharp
// Query - returns list
Task<IEnumerable<T>> StatementExecuteQueryAsync<T>(
    string statementId,
    object? parameters = null)

// Query - returns single or default
Task<T?> StatementExecuteSingleAsync<T>(
    string statementId,
    object? parameters = null)

// Query - returns first or default
Task<T?> StatementExecuteFirstAsync<T>(
    string statementId,
    object? parameters = null)

// Scalar - returns single value (COUNT, SUM, etc.)
Task<T> StatementExecuteScalarAsync<T>(
    string statementId,
    object? parameters = null)

// Execute - INSERT, UPDATE, DELETE
Task<int> StatementExecuteAsync(
    string statementId,
    object? parameters = null)

// Transaction support
Task ExecuteInTransactionAsync(
    Func<ISqlConnection, Task> action)

Task<TResult> ExecuteInTransactionAsync<TResult>(
    Func<ISqlConnection, Task<TResult>> func)
```

---

## 💡 Patterns & Examples

### Pattern 1: Basic CRUD

```csharp
public class AssetService
{
    private readonly ISql _sql;

    // GET ALL
    public async Task<List<Asset>> GetAllAsync()
    {
        _sql.GetDAO(Provider.DAO000);
        using var conn = _sql.CreateConnection();
        return (await conn.StatementExecuteQueryAsync<Asset>("Asset.GetAll")).ToList();
    }

    // GET BY ID
    public async Task<Asset?> GetByIdAsync(int id)
    {
        _sql.GetDAO(Provider.DAO000);
        using var conn = _sql.CreateConnection();
        return await conn.StatementExecuteSingleAsync<Asset>(
            "Asset.GetById",
            new { Id = id });
    }

    // COUNT
    public async Task<int> CountAsync(string? keyword = null)
    {
        _sql.GetDAO(Provider.DAO000);
        using var conn = _sql.CreateConnection();
        return await conn.StatementExecuteScalarAsync<int>(
            "Asset.Count",
            new { SearchKeyword = keyword });
    }

    // INSERT
    public async Task<int> CreateAsync(Asset asset)
    {
        _sql.GetDAO(Provider.DAO000);
        using var conn = _sql.CreateConnection();
        return await conn.StatementExecuteAsync("Asset.Insert", asset);
    }

    // UPDATE
    public async Task<int> UpdateAsync(Asset asset)
    {
        _sql.GetDAO(Provider.DAO000);
        using var conn = _sql.CreateConnection();
        return await conn.StatementExecuteAsync("Asset.Update", asset);
    }

    // DELETE
    public async Task<int> DeleteAsync(int id)
    {
        _sql.GetDAO(Provider.DAO000);
        using var conn = _sql.CreateConnection();
        return await conn.StatementExecuteAsync("Asset.Delete", new { Id = id });
    }
}
```

### Pattern 2: Pagination

```csharp
public async Task<List<Asset>> GetPaginatedAsync(
    int page,
    int pageSize,
    string? searchKeyword = null,
    int? categoryId = null)
{
    _sql.GetDAO(Provider.DAO000);

    var offset = (page - 1) * pageSize;

    using var conn = _sql.CreateConnection();
    var assets = await conn.StatementExecuteQueryAsync<Asset>(
        "Asset.GetPaginated",
        new
        {
            Offset = offset,
            PageSize = pageSize,
            SearchKeyword = searchKeyword,
            CategoryId = categoryId
        });

    return assets.ToList();
}
```

### Pattern 3: Cross-DAO Operations

```csharp
/// <summary>
/// Get asset with category (2 DAOs)
/// </summary>
public async Task<AssetWithCategory> GetAssetWithCategoryAsync(int assetId)
{
    // Get asset from DAO000
    _sql.GetDAO(Provider.DAO000);
    using var conn1 = _sql.CreateConnection();
    var asset = await conn1.StatementExecuteSingleAsync<Asset>(
        "Asset.GetById",
        new { Id = assetId });

    if (asset == null)
        throw new InvalidOperationException($"Asset {assetId} not found");

    // Get category from DAO001
    Category? category = null;
    if (asset.CategoryId > 0)
    {
        _sql.GetDAO(Provider.DAO001);
        using var conn2 = _sql.CreateConnection();
        category = await conn2.StatementExecuteSingleAsync<Category>(
            "Category.GetById",
            new { Id = asset.CategoryId });
    }

    return new AssetWithCategory
    {
        Asset = asset,
        Category = category
    };
}
```

### Pattern 4: Multiple Connections (MainDB vs ReportDB)

```csharp
public class AssetService
{
    private readonly ISql _sql;

    // MainDB - Read/Write operations
    public async Task<Asset?> GetAssetAsync(int id)
    {
        // DAO000 from MainDB
        _sql.GetDAO(Provider.DAO000, "MainDB");
        using var conn = _sql.CreateConnection("MainDB");
        return await conn.StatementExecuteSingleAsync<Asset>(
            "Asset.GetById",
            new { Id = id });
    }

    public async Task<int> CreateAssetAsync(Asset asset)
    {
        _sql.GetDAO(Provider.DAO000, "MainDB");
        using var conn = _sql.CreateConnection("MainDB");
        return await conn.StatementExecuteAsync("Asset.Insert", asset);
    }

    // ReportDB - Read-only operations
    public async Task<List<Asset>> GetAssetsForReportAsync()
    {
        // DAO000 from ReportDB (read replica)
        _sql.GetDAO(Provider.DAO000, "ReportDB");
        using var conn = _sql.CreateConnection("ReportDB");
        var assets = await conn.StatementExecuteQueryAsync<Asset>("Asset.GetAll");
        return assets.ToList();
    }

    public async Task<AssetSummary?> GetSummaryAsync()
    {
        _sql.GetDAO(Provider.DAO000, "ReportDB");
        using var conn = _sql.CreateConnection("ReportDB");
        return await conn.StatementExecuteSingleAsync<AssetSummary>("Asset.GetSummary");
    }
}
```

### Pattern 5: Transaction Support

```csharp
/// <summary>
/// Create asset with history in transaction
/// </summary>
public async Task<int> CreateAssetWithHistoryAsync(Asset asset, string createdBy)
{
    _sql.GetDAO(Provider.DAO000);
    using var conn = _sql.CreateConnection();

    return await conn.ExecuteInTransactionAsync(async c =>
    {
        // 1. Insert asset
        var assetId = await c.StatementExecuteAsync("Asset.Insert", asset);

        // 2. Insert history
        await c.StatementExecuteAsync(
            "Asset.InsertHistory",
            new
            {
                AssetId = assetId,
                Action = "Created",
                ActionBy = createdBy,
                ActionDate = DateTime.Now
            });

        return assetId;
    });
}
```

---

## 🔍 So sánh: ISql vs Repository Pattern

| Feature | ISql Pattern | Repository Pattern |
|---------|--------------|-------------------|
| **Code Style** | Procedural, iBatis.NET style | OOP, Domain-driven |
| **Setup** | Inject `ISql` | Inherit base class |
| **DAO Switching** | `_sql.GetDAO(...)` in method | Constructor-time only |
| **Connection Switching** | Easy per-method | Requires separate repo |
| **CRUD Operations** | `conn.StatementExecuteQueryAsync(...)` | `await QueryListAsync(...)` |
| **Learning Curve** | Lower (familiar to iBatis users) | Medium (OOP concepts) |
| **Boilerplate** | Less (no repo classes) | More (1 repo per entity) |
| **Use Case** | Simple services, migrations | Complex domain logic |

**Khuyến nghị**:
- ✅ **ISql**: Services đơn giản, prototype, migration từ iBatis.NET
- ✅ **Repository**: Domain logic phức tạp, enterprise apps, testability cao

---

## 🎯 Use Cases

### Use Case 1: Simple Service (1 DAO)

```csharp
public class CategoryService
{
    private readonly ISql _sql;

    public CategoryService(ISql sql)
    {
        _sql = sql;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        _sql.GetDAO(Provider.DAO001); // Categories
        using var conn = _sql.CreateConnection();
        return (await conn.StatementExecuteQueryAsync<Category>("Category.GetAll")).ToList();
    }
}
```

### Use Case 2: Complex Service (Multiple DAOs)

```csharp
public class CheckSessionService
{
    private readonly ISql _sql;

    public async Task<CheckSessionDetail> GetDetailAsync(int sessionId)
    {
        // Get session from DAO003
        _sql.GetDAO(Provider.DAO003);
        using var conn1 = _sql.CreateConnection();
        var session = await conn1.StatementExecuteSingleAsync<CheckSession>(
            "CheckSession.GetById",
            new { Id = sessionId });

        // Get items from DAO004
        _sql.GetDAO(Provider.DAO004);
        using var conn2 = _sql.CreateConnection();
        var items = await conn2.StatementExecuteQueryAsync<CheckItem>(
            "CheckItem.GetBySession",
            new { SessionId = sessionId });

        // Get assets from DAO000
        var assetIds = items.Select(i => i.AssetId).Distinct();
        _sql.GetDAO(Provider.DAO000);
        using var conn3 = _sql.CreateConnection();
        var assets = await conn3.StatementExecuteQueryAsync<Asset>(
            "Asset.GetByIds",
            new { AssetIds = assetIds });

        return new CheckSessionDetail
        {
            Session = session,
            Items = items.ToList(),
            Assets = assets.ToList()
        };
    }
}
```

### Use Case 3: Report Service (Report Database)

```csharp
public class ReportService
{
    private readonly ISql _sql;

    public async Task<AssetReport> GenerateAssetReportAsync(DateTime fromDate, DateTime toDate)
    {
        // All queries go to ReportDB (read replica)
        _sql.GetDAO(Provider.DAO009, "ReportDB");
        using var conn = _sql.CreateConnection("ReportDB");

        var report = new AssetReport();

        report.Summary = await conn.StatementExecuteSingleAsync<ReportSummary>(
            "Report.GetAssetSummary",
            new { FromDate = fromDate, ToDate = toDate });

        report.Details = (await conn.StatementExecuteQueryAsync<ReportDetail>(
            "Report.GetAssetDetails",
            new { FromDate = fromDate, ToDate = toDate })).ToList();

        return report;
    }
}
```

---

## ⚙️ Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "MainDB": "Server=main-server;Database=SmartCheck;User Id=sa;Password=***;",
    "ReportDB": "Server=report-server;Database=SmartCheck_Report;User Id=reader;Password=***;"
  }
}
```

### Program.cs

```csharp
var mainConnection = builder.Configuration.GetConnectionString("MainDB");
var reportConnection = builder.Configuration.GetConnectionString("ReportDB");

builder.Services.AddWscDataAccess(mainConnection, options =>
{
    options.AddConnection("MainDB", mainConnection);
    options.AddConnection("ReportDB", reportConnection);

    options.ConfigureSqlMaps(provider =>
    {
        // MainDB
        provider.AddFile(Provider.DAO000, "DAO/DAO000.xml", "MainDB");
        provider.AddFile(Provider.DAO001, "DAO/DAO001.xml", "MainDB");

        // ReportDB
        provider.AddFile(Provider.DAO000, "DAO/DAO000.xml", "ReportDB");
        provider.AddFile(Provider.DAO009, "DAO/DAO009.xml", "ReportDB");
    });
});
```

---

## ✅ Best Practices

### 1. Always Call GetDAO Before CreateConnection

```csharp
// ✅ GOOD
_sql.GetDAO(Provider.DAO000);
using var conn = _sql.CreateConnection();

// ❌ BAD - will throw exception
using var conn = _sql.CreateConnection(); // No DAO context!
```

### 2. Use `using` for Connections

```csharp
// ✅ GOOD - connection auto-closed
using var conn = _sql.CreateConnection();
var result = await conn.StatementExecuteQueryAsync<Asset>("Asset.GetAll");

// ❌ BAD - connection leak
var conn = _sql.CreateConnection();
var result = await conn.StatementExecuteQueryAsync<Asset>("Asset.GetAll");
// conn never closed!
```

### 3. Handle Exceptions

```csharp
public async Task<Asset?> GetAssetAsync(int id)
{
    try
    {
        _sql.GetDAO(Provider.DAO000);
        using var conn = _sql.CreateConnection();
        return await conn.StatementExecuteSingleAsync<Asset>(
            "Asset.GetById",
            new { Id = id });
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
    {
        // DAO not registered
        _logger.LogError(ex, "DAO configuration error");
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting asset {AssetId}", id);
        throw;
    }
}
```

### 4. Use Transactions for Multi-Statement Operations

```csharp
// ✅ GOOD - transaction ensures atomicity
public async Task<int> TransferAssetAsync(int assetId, int newLocationId)
{
    _sql.GetDAO(Provider.DAO000);
    using var conn = _sql.CreateConnection();

    return await conn.ExecuteInTransactionAsync(async c =>
    {
        await c.StatementExecuteAsync("Asset.UpdateLocation",
            new { AssetId = assetId, LocationId = newLocationId });

        await c.StatementExecuteAsync("Asset.InsertHistory",
            new { AssetId = assetId, Action = "Transferred" });

        return assetId;
    });
}

// ❌ BAD - no transaction, inconsistent state if second statement fails
public async Task<int> TransferAssetAsync(int assetId, int newLocationId)
{
    _sql.GetDAO(Provider.DAO000);
    using var conn = _sql.CreateConnection();

    await conn.StatementExecuteAsync("Asset.UpdateLocation",
        new { AssetId = assetId, LocationId = newLocationId });

    await conn.StatementExecuteAsync("Asset.InsertHistory",
        new { AssetId = assetId, Action = "Transferred" }); // If this fails, asset updated but no history!

    return assetId;
}
```

---

## 📚 Related Documentation

- [DAONAMES_MAPPING_GUIDE.md](DAONAMES_MAPPING_GUIDE.md) - DaoNames pattern
- [MULTIPLE_CONNECTIONS_DAONAMES.md](MULTIPLE_CONNECTIONS_DAONAMES.md) - Multiple connections
- [REPOSITORY_PATTERNS_SUMMARY.md](REPOSITORY_PATTERNS_SUMMARY.md) - Repository patterns comparison

---

## 🎉 Summary

✅ **ISql Pattern** = iBatis.NET style API
✅ **Simple & Familiar** = Dễ migration, ít boilerplate
✅ **Flexible** = Switch DAO/connection per method
✅ **Powerful** = Transaction support, cross-DAO operations

**Chúc bạn coding vui vẻ! 🚀**
