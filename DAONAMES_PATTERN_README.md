# DaoNames Mapping Pattern - Quick Start

## 🎯 Tổng quan

Pattern này cho phép **project sử dụng** (consuming project) tự định nghĩa `DaoNames` và ánh xạ về WSC.DataAccess, giống như pattern trong **MrFu.SmartCheck.Web**.

## 🚀 Quick Start (3 Bước)

### Bước 1: Khai báo DaoNames trong Project

**File: `Models/Provider.cs`**

```csharp
namespace MrFu.SmartCheck.Web.Models;

public static class Provider
{
    public static readonly string DAO000 = "DAO000"; // Assets
    public static readonly string DAO001 = "DAO001"; // Categories
    public static readonly string DAO002 = "DAO002"; // Locations
    public static readonly string DAO003 = "DAO003"; // CheckSessions
    // ... thêm các DAOs khác

    public static string[] GetAllDaoNames()
    {
        return typeof(Provider)
            .GetFields()
            .Where(f => f.IsStatic && f.IsInitOnly && f.Name.StartsWith("DAO"))
            .Select(f => f.GetValue(null)?.ToString())
            .Where(v => v != null)
            .ToArray()!;
    }
}
```

### Bước 2: Ánh xạ trong Program.cs

```csharp
using MrFu.SmartCheck.Web.Models;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddWscDataAccess(connectionString, options =>
{
    options.ConfigureSqlMaps(provider =>
    {
        // Ánh xạ DaoNames → File paths
        provider.AddFile(Provider.DAO000, "DAO/DAO000.xml", "Assets");
        provider.AddFile(Provider.DAO001, "DAO/DAO001.xml", "Categories");
        provider.AddFile(Provider.DAO002, "DAO/DAO002.xml", "Locations");
        provider.AddFile(Provider.DAO003, "DAO/DAO003.xml", "CheckSessions");
        // ... thêm các DAOs khác
    });
});
```

### Bước 3: Sử dụng trong Service

#### Option A: Service dùng 1 DAO

```csharp
using WSC.DataAccess.Repository;
using WSC.DataAccess.Configuration;

public class AssetService : ProviderBasedRepository<Asset>
{
    private const string DAO_NAME = Provider.DAO000;

    public AssetService(
        IDbSessionFactory factory,
        SqlMapProvider provider)
        : base(factory, provider, DAO_NAME)
    {
    }

    public async Task<IEnumerable<Asset>> GetAllAsync()
    {
        return await QueryListAsync("Asset.GetAll");
    }
}
```

#### Option B: Service dùng NHIỀU DAOs

```csharp
using WSC.DataAccess.Repository;
using WSC.DataAccess.Configuration;

public class CheckSessionService : MultiDaoProviderRepository<dynamic>
{
    private static readonly string[] DAO_NAMES = new[]
    {
        Provider.DAO003,  // CheckSessions
        Provider.DAO004,  // CheckItems
        Provider.DAO000,  // Assets
        Provider.DAO006   // Alerts
    };

    public CheckSessionService(
        IDbSessionFactory factory,
        SqlMapProvider provider)
        : base(factory, provider, DAO_NAMES)
    {
    }

    // Methods có thể dùng statements từ TẤT CẢ 4 DAOs
    public async Task<CheckSession?> GetSessionAsync(int id)
    {
        return await QuerySingleAsync<CheckSession>(
            "CheckSession.GetById", new { Id = id });
    }

    public async Task<IEnumerable<CheckItem>> GetItemsAsync(int sessionId)
    {
        return await QueryListAsync<CheckItem>(
            "CheckItem.GetBySession", new { SessionId = sessionId });
    }

    public async Task<Asset?> GetAssetAsync(int assetId)
    {
        return await QuerySingleAsync<Asset>(
            "Asset.GetById", new { Id = assetId });
    }
}
```

## 📋 So sánh 2 Patterns

| Feature | Single DAO | Multiple DAOs |
|---------|------------|---------------|
| **Base Class** | `ProviderBasedRepository<T>` | `MultiDaoProviderRepository<T>` |
| **Constructor** | `base(factory, provider, DAO_NAME)` | `base(factory, provider, DAO_NAMES[])` |
| **Use Case** | Service đơn giản, 1 domain | Service phức tạp, nhiều domains |
| **Example** | UserService, ProductService | CheckSessionService, ReportService |
| **Load Files** | 1 file (DAO001.xml) | Nhiều files (DAO001, DAO002, DAO003) |

## 🎯 Khi nào dùng pattern nào?

### ✅ Dùng Single DAO (ProviderBasedRepository)

- Service chỉ quản lý 1 domain/entity
- Example: `AssetService` chỉ cần `DAO000` (Assets)
- Example: `UserService` chỉ cần `DAO001` (Users)

### ✅ Dùng Multiple DAOs (MultiDaoProviderRepository)

- Service cần truy cập nhiều domains
- Example: `CheckSessionService` cần `DAO003` (Sessions) + `DAO004` (Items) + `DAO000` (Assets)
- Example: `ReportService` cần nhiều DAO reports: `DAO009`, `DAO010`, `DAO011`, v.v.
- Business logic phức tạp với cross-domain transactions

## 📁 Cấu trúc Project

```
MrFu.SmartCheck.Web/
├── Models/
│   └── Provider.cs                    ← Khai báo DaoNames
│
├── Services/
│   ├── AssetService.cs               ← Single DAO (DAO000)
│   ├── CategoryService.cs            ← Single DAO (DAO001)
│   ├── CheckSessionService.cs        ← Multiple DAOs (003, 004, 000)
│   └── ReportService.cs              ← Multiple DAOs (009-013)
│
├── DAO/
│   ├── DAO000.xml                    ← Asset SQL statements
│   ├── DAO001.xml                    ← Category SQL statements
│   ├── DAO003.xml                    ← CheckSession SQL statements
│   └── ...
│
└── Program.cs                         ← ConfigureSqlMaps()
```

## ✨ Lợi ích

✅ **IntelliSense Support**: `Provider.DAO000` thay vì `"DAO000"`
✅ **Type Safety**: Compiler check, không bị typo
✅ **Centralized**: Tất cả DAO names ở 1 nơi
✅ **Flexible**: Project tự định nghĩa DaoNames theo domain
✅ **Maintainable**: Dễ dàng thay đổi file locations
✅ **Scalable**: Dễ dàng thêm DAO mới

## 📚 Chi tiết

Xem tài liệu đầy đủ tại:
- [DAONAMES_MAPPING_GUIDE.md](DAONAMES_MAPPING_GUIDE.md) - Hướng dẫn chi tiết
- [samples/WSC.DataAccess.Sample/ProviderPatternDemo.cs](samples/WSC.DataAccess.Sample/ProviderPatternDemo.cs) - Demo code

## 🔧 Classes mới

### MultiDaoProviderRepository<T>

Base class hỗ trợ load **nhiều DAO files** từ Provider pattern.

**File**: `src/WSC.DataAccess/Repository/MultiDaoProviderRepository.cs`

**Features**:
- Load nhiều SQL map files từ provider
- Hỗ trợ cross-domain queries
- Transaction support across multiple DAOs
- Generic type support cho mixed entities

## 📝 Sample Code

### Sample 1: Provider Definition

Xem: `samples/WSC.DataAccess.Sample/Models/Provider.cs`

### Sample 2: Simple Service (1 DAO)

Xem: `samples/WSC.DataAccess.Sample/Services/SimpleUserService.cs`

### Sample 3: Complex Service (Multiple DAOs)

Xem: `samples/WSC.DataAccess.Sample/Services/ComplexBusinessService.cs`

### Sample 4: Complete Demo

Xem: `samples/WSC.DataAccess.Sample/ProviderPatternDemo.cs`

## ❓ FAQ

### Q: Có thể dùng tên khác "DAO000" không?

**A**: Có! Key có thể là bất kỳ string nào:

```csharp
public static class Provider
{
    public static readonly string ASSETS = "Assets";
    public static readonly string USERS = "Users";
}

// Program.cs
provider.AddFile(Provider.ASSETS, "DAO/Assets.xml");
```

### Q: Service có thể dùng DAOs từ nhiều connections khác nhau không?

**A**: Có! Xem [MULTI_CONNECTION_GUIDE.md](MULTI_CONNECTION_GUIDE.md)

### Q: Làm sao validate tất cả DAOs đã được register?

**A**: Thêm code này trong Program.cs:

```csharp
var provider = app.Services.GetRequiredService<SqlMapProvider>();
var missingDaos = Provider.GetAllDaoNames()
    .Where(dao => !provider.HasFile(dao))
    .ToArray();

if (missingDaos.Any())
{
    throw new InvalidOperationException(
        $"Missing DAOs: {string.Join(", ", missingDaos)}");
}
```

## 🎉 Summary

Pattern này cho phép bạn:

1. ✅ Tự định nghĩa DaoNames trong project
2. ✅ Ánh xạ DaoNames → SQL files trong Program.cs
3. ✅ Service dùng 1 DAO: `ProviderBasedRepository<T>`
4. ✅ Service dùng nhiều DAOs: `MultiDaoProviderRepository<T>`
5. ✅ IntelliSense + Type Safety + Centralized config

**Chúc bạn coding vui vẻ! 🚀**
