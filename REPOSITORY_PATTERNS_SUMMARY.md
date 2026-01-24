# Repository Patterns Summary - Chọn Pattern Nào?

Hướng dẫn chọn pattern phù hợp cho dự án của bạn.

---

## 📋 Tất Cả Patterns Available

WSC.DataAccess hỗ trợ **4 patterns** để khai báo SQL map files:

1. **Hardcoded Pattern** (❌ Not recommended)
2. **Constants Pattern** (✅ Good)
3. **Attribute Pattern** (⭐ Simple & Quick)
4. **Provider Pattern** (⭐⭐ Enterprise & Scalable)

---

## 🆚 Quick Comparison

| Feature | Hardcoded | Constants | Attribute | **Provider** |
|---------|-----------|-----------|-----------|--------------|
| **Centralized config** | ❌ | ❌ | ❌ | **✅✅** |
| **IntelliSense support** | ❌ | ✅ | ✅ | ✅ |
| **Type-safe** | ❌ | ✅ | ✅ | ✅ |
| **Easy to change** | ❌ | ⚠️ | ⚠️ | **✅✅** |
| **Lines of code** | 7 | 7 | **4** | 5 |
| **Maintenance** | Hard | Medium | Medium | **Easy** |
| **Scalability** | Poor | Medium | Medium | **Excellent** |
| **Best for** | ❌ Never | Small | Quick | **Enterprise** |

---

## 1️⃣ Hardcoded Pattern (❌ DON'T USE)

### Code Example

```csharp
public class OrderRepository : ScopedSqlMapRepository<Order>
{
    private const string SQL_MAP_FILE = "SqlMaps/DAO005.xml";  // ❌ Hardcoded string

    public OrderRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory, SQL_MAP_FILE)
    {
    }
}
```

### ❌ Problems
- **No IntelliSense**: Dễ typo
- **Not type-safe**: Compiler không kiểm tra
- **Hard to refactor**: Phải tìm/thay từng chỗ
- **Scattered**: File paths ở khắp nơi

### When to Use
- **NEVER!** ❌

### Guide
- N/A (Không khuyến khích)

---

## 2️⃣ Constants Pattern (✅ GOOD)

### Code Example

```csharp
using WSC.DataAccess.Constants;

public class OrderRepository : ScopedSqlMapRepository<Order>
{
    private const string SQL_MAP_FILE = SqlMapFiles.DAO005;  // ✅ Constant

    public OrderRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory, SQL_MAP_FILE)
    {
    }
}
```

### ✅ Benefits
- **IntelliSense**: Auto-complete khi gõ `SqlMapFiles.`
- **Type-safe**: Compiler kiểm tra
- **Refactor-friendly**: Rename constant = rename everywhere

### ⚠️ Limitations
- Vẫn phải khai báo `SQL_MAP_FILE` ở mỗi repository
- Không tập trung

### When to Use
- ✅ Small projects (< 10 repositories)
- ✅ Simple requirements
- ✅ Quick prototypes

### Guide
- **SQLMAP_CONSTANTS_GUIDE.md**

---

## 3️⃣ Attribute Pattern (⭐ SIMPLE & QUICK)

### Code Example

```csharp
using WSC.DataAccess.Attributes;
using WSC.DataAccess.Constants;

[SqlMapFile(SqlMapFiles.DAO005)]  // ← CHỈ DÒNG NÀY!
public class OrderRepository : SimpleSqlMapRepository<Order>
{
    public OrderRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory) { }
}
```

**Just 4 lines!** 🎉

### Multiple Files

```csharp
[SqlMapFiles(SqlMapFiles.DAO005, SqlMapFiles.DAO010)]
public class ReportRepository : SimpleSqlMapRepository<dynamic>
{
    public OrderRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory) { }
}
```

### ✅ Benefits
- **Simplest code**: Chỉ 4 dòng!
- **Declarative**: Dễ đọc, dễ hiểu
- **IntelliSense**: Full support
- **Type-safe**: Compiler kiểm tra

### ⚠️ Limitations
- File path vẫn ở trong repository (không tập trung)
- Khó thay đổi file path cho nhiều repositories cùng lúc

### When to Use
- ✅ **Quick development** (recommended!)
- ✅ 1 repository = 1 file cố định
- ✅ Không cần logic phức tạp
- ✅ Solo developer hoặc small team

### Guide
- **SIMPLE_REPOSITORY_GUIDE.md**

---

## 4️⃣ Provider Pattern (⭐⭐ ENTERPRISE & SCALABLE)

### Code Example

#### Program.cs (Khai báo TẬP TRUNG)

```csharp
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Constants;

builder.Services.AddWscDataAccess(connectionString, options =>
{
    // ✨ Khai báo TẤT CẢ SQL maps ở MỘT CHỖ!
    options.ConfigureSqlMaps(provider =>
    {
        provider.AddFile("Order", SqlMapFiles.DAO005, "Order management");
        provider.AddFile("Customer", SqlMapFiles.DAO010, "Customer data");
        provider.AddFile("Product", SqlMapFiles.DAO015, "Product catalog");
        provider.AddFile("Inventory", SqlMapFiles.DAO016, "Inventory tracking");
        provider.AddFile("Report", SqlMapFiles.DAO020, "Business reports");
    });
});
```

#### Repository (Chỉ dùng KEY)

```csharp
using WSC.DataAccess.Configuration;

public class OrderRepository : ProviderBasedRepository<Order>
{
    private const string MAP_KEY = "Order";  // ← Chỉ cần KEY!

    public OrderRepository(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider)
        : base(sessionFactory, provider, MAP_KEY)
    {
        // File path tự động lấy từ provider.GetFilePath("Order")
    }
}
```

### ✅ Benefits
- **Centralized**: Tất cả SQL maps khai báo ở MỘT CHỖ (Program.cs)
- **Scalable**: Dễ quản lý hàng chục/trăm repositories
- **Easy to change**: Đổi file path ở 1 chỗ = đổi toàn bộ
- **Enterprise-ready**: Professional pattern
- **IntelliSense**: Full support
- **Type-safe**: Compiler kiểm tra
- **Like MrFu.Smartcheck**: Giống pattern quen thuộc!

### ⚠️ Limitations
- Hơi phức tạp hơn Attribute pattern (nhưng worth it!)
- Cần hiểu DI/IoC pattern

### When to Use
- ✅✅ **Enterprise applications** (recommended!)
- ✅✅ Large projects (> 10 repositories)
- ✅✅ Team development
- ✅✅ Cần centralized configuration
- ✅✅ Muốn dễ maintain/scale

### Guide
- **PROVIDER_PATTERN_GUIDE.md**

---

## 🎯 Decision Tree - Chọn Pattern Nào?

```
START
  │
  ├─ Dự án nhỏ, quick prototype?
  │  └─ YES → ⭐ ATTRIBUTE PATTERN (4 lines, super fast!)
  │
  ├─ Enterprise app, nhiều repositories?
  │  └─ YES → ⭐⭐ PROVIDER PATTERN (centralized, scalable!)
  │
  ├─ Team lớn, cần maintain lâu dài?
  │  └─ YES → ⭐⭐ PROVIDER PATTERN
  │
  ├─ Muốn pattern giống MrFu.Smartcheck?
  │  └─ YES → ⭐⭐ PROVIDER PATTERN
  │
  ├─ Solo developer, simple app?
  │  └─ YES → ⭐ ATTRIBUTE PATTERN
  │
  └─ Không chắc chọn gì?
     └─ Default → ⭐ ATTRIBUTE PATTERN (đơn giản nhất!)
```

---

## 📊 Use Case Examples

### Use Case 1: Startup MVP

**Scenario**: Startup cần MVP nhanh, 5-10 repositories, 1-2 developers

**Recommended**: ⭐ **Attribute Pattern**

**Why?**
- ✅ Code ít nhất (4 lines)
- ✅ Dễ hiểu, dễ dùng
- ✅ Đủ tốt cho MVP
- ✅ Có thể migrate sang Provider sau

```csharp
[SqlMapFile(SqlMapFiles.DAO005)]
public class OrderRepository : SimpleSqlMapRepository<Order>
{
    public OrderRepository(IDbSessionFactory sf) : base(sf) { }
}
```

---

### Use Case 2: Enterprise E-Commerce

**Scenario**: E-commerce lớn, 50+ repositories, team 10+ developers

**Recommended**: ⭐⭐ **Provider Pattern**

**Why?**
- ✅ Centralized config dễ quản lý
- ✅ Team lead kiểm soát SQL maps
- ✅ Dễ refactor/reorganize
- ✅ Professional, scalable

```csharp
// Program.cs - Team lead khai báo
options.ConfigureSqlMaps(provider =>
{
    // Order Domain (5 files)
    provider.AddFile("Order.Main", SqlMapFiles.DAO005);
    provider.AddFile("Order.Items", SqlMapFiles.DAO006);
    // ... 3 more

    // Customer Domain (8 files)
    provider.AddFile("Customer.Profile", SqlMapFiles.DAO010);
    provider.AddFile("Customer.Address", SqlMapFiles.DAO011);
    // ... 6 more

    // Product Domain (12 files)
    // ... and so on
});

// Repository - Dev chỉ cần dùng key
public class OrderRepository : ProviderBasedRepository<Order>
{
    private const string MAP_KEY = "Order.Main";
    // ...
}
```

---

### Use Case 3: Microservices

**Scenario**: Microservice architecture, mỗi service có 3-5 repositories

**Recommended**: ⭐ **Attribute Pattern** hoặc ⭐⭐ **Provider Pattern**

**Attribute nếu**:
- Service nhỏ, đơn giản
- Ít thay đổi

**Provider nếu**:
- Service phức tạp
- Nhiều SQL maps
- Cần flexibility

---

### Use Case 4: Legacy Migration

**Scenario**: Migrate legacy codebase sang WSC.DataAccess

**Recommended**: Start with ⭐ **Attribute**, migrate to ⭐⭐ **Provider** sau

**Phase 1**: Dùng Attribute để migrate nhanh
```csharp
[SqlMapFile(SqlMapFiles.DAO005)]
public class OrderRepository : SimpleSqlMapRepository<Order> { }
```

**Phase 2**: Khi đã stable, migrate sang Provider
```csharp
// Program.cs
options.ConfigureSqlMaps(provider =>
{
    provider.AddFile("Order", SqlMapFiles.DAO005);
});

// Repository
public class OrderRepository : ProviderBasedRepository<Order>
{
    private const string MAP_KEY = "Order";
}
```

---

## 🔄 Migration Between Patterns

### From Hardcoded → Constants

```diff
- private const string SQL_MAP_FILE = "SqlMaps/DAO005.xml";
+ private const string SQL_MAP_FILE = SqlMapFiles.DAO005;
```

### From Constants → Attribute

```diff
+ [SqlMapFile(SqlMapFiles.DAO005)]
- public class OrderRepository : ScopedSqlMapRepository<Order>
+ public class OrderRepository : SimpleSqlMapRepository<Order>
  {
-     private const string SQL_MAP_FILE = SqlMapFiles.DAO005;

      public OrderRepository(IDbSessionFactory sf)
-         : base(sf, SQL_MAP_FILE) { }
+         : base(sf) { }
  }
```

### From Attribute → Provider

**Step 1**: Add to Program.cs
```csharp
options.ConfigureSqlMaps(provider =>
{
    provider.AddFile("Order", SqlMapFiles.DAO005);
});
```

**Step 2**: Update Repository
```diff
- [SqlMapFile(SqlMapFiles.DAO005)]
- public class OrderRepository : SimpleSqlMapRepository<Order>
+ public class OrderRepository : ProviderBasedRepository<Order>
  {
+     private const string MAP_KEY = "Order";

-     public OrderRepository(IDbSessionFactory sf)
-         : base(sf) { }
+     public OrderRepository(IDbSessionFactory sf, SqlMapProvider provider)
+         : base(sf, provider, MAP_KEY) { }
  }
```

---

## 📚 All Guides

1. **SQLMAP_CONSTANTS_GUIDE.md** - Constants Pattern
2. **SIMPLE_REPOSITORY_GUIDE.md** - Attribute Pattern (simplest!)
3. **PROVIDER_PATTERN_GUIDE.md** - Provider Pattern (enterprise!)
4. **SCOPED_SQLMAP_GUIDE.md** - Scoped SQL Maps
5. **IBATIS_LOGGING.md** - Logging configuration
6. **LOGGING_TEST_GUIDE.md** - Testing logs
7. **LOG_EXAMPLES.md** - Log examples quick reference

---

## 💡 Recommendations

### For New Projects

**Small/Medium (< 10 repos)**:
- Start with ⭐ **Attribute Pattern**
- Migrate to Provider if needed later

**Large/Enterprise (> 10 repos)**:
- Start with ⭐⭐ **Provider Pattern** from day 1
- Save time in the long run

### For Existing Projects

**If you have Hardcoded**:
1. Migrate to Constants first (easy)
2. Then consider Attribute or Provider

**If you have Constants**:
- Small project → Migrate to Attribute (less code)
- Large project → Migrate to Provider (better management)

---

## ✅ Final Recommendation

### Default Choice: ⭐ Attribute Pattern

**Why?**
- Simplest (4 lines)
- Quick to implement
- Good for 80% of projects
- Easy to understand

### When to Upgrade: ⭐⭐ Provider Pattern

**When you notice**:
- Có > 10 repositories
- Khó quản lý SQL map files
- Team lớn, cần coordination
- Muốn centralized control

---

## 🎯 TL;DR

**Quick Start**:
```csharp
// ⭐ SIMPLEST (4 lines) - Recommended for most projects
[SqlMapFile(SqlMapFiles.DAO005)]
public class OrderRepository : SimpleSqlMapRepository<Order>
{
    public OrderRepository(IDbSessionFactory sf) : base(sf) { }
}
```

**Enterprise**:
```csharp
// ⭐⭐ CENTRALIZED - Recommended for large projects

// Program.cs
options.ConfigureSqlMaps(provider =>
{
    provider.AddFile("Order", SqlMapFiles.DAO005);
});

// Repository
public class OrderRepository : ProviderBasedRepository<Order>
{
    private const string MAP_KEY = "Order";
    public OrderRepository(IDbSessionFactory sf, SqlMapProvider provider)
        : base(sf, provider, MAP_KEY) { }
}
```

**Choose based on your project size!** 🚀

---

✅ **Done!** Tất cả patterns đã được implement và documented! 🎉
