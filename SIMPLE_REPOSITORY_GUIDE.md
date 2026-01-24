# Simple Repository Guide - Đơn Giản Nhất!

Hướng dẫn tạo repository chỉ với 3 dòng code!

## ✨ Cách Đơn Giản Nhất

### 1 File:

```csharp
using WSC.DataAccess.Attributes;
using WSC.DataAccess.Constants;
using WSC.DataAccess.Repository;

[SqlMapFile(SqlMapFiles.DAO005)]  // ← CHỈ CẦN DÒNG NÀY!
public class OrderRepository : SimpleSqlMapRepository<Order>
{
    public OrderRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory) { }
}
```

**That's it!** 🎉

---

### Nhiều Files:

```csharp
[SqlMapFiles(SqlMapFiles.DAO005, SqlMapFiles.DAO010)]  // ← List files cần dùng
public class ReportRepository : SimpleSqlMapRepository<dynamic>
{
    public ReportRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory) { }
}
```

**Done!** ✅

---

## 🆚 So Sánh Các Cách

### Cách 1: Hardcoded (Cũ - Không khuyến khích)

```csharp
public class OrderRepository : ScopedSqlMapRepository<Order>
{
    private const string SQL_MAP_FILE = "SqlMaps/DAO005.xml";  // ❌ Hardcoded

    public OrderRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory, SQL_MAP_FILE)
    {
    }
}
```

- ❌ Dễ typo
- ❌ Không IntelliSense
- Lines of code: **7 dòng**

---

### Cách 2: Constants (Tốt hơn)

```csharp
public class OrderRepository : ScopedSqlMapRepository<Order>
{
    private const string SQL_MAP_FILE = SqlMapFiles.DAO005;  // ✅ Constant

    public OrderRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory, SQL_MAP_FILE)
    {
    }
}
```

- ✅ IntelliSense
- ✅ Type-safe
- Lines of code: **7 dòng**

---

### Cách 3: Attribute (ĐƠN GIẢN NHẤT!) ⭐

```csharp
[SqlMapFile(SqlMapFiles.DAO005)]  // ← CHỈ DÒNG NÀY!
public class OrderRepository : SimpleSqlMapRepository<Order>
{
    public OrderRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory) { }
}
```

- ✅ IntelliSense
- ✅ Type-safe
- ✅ **ÍT CODE NHẤT**
- Lines of code: **4 dòng**

**Winner!** 🏆

---

## 📝 Complete Example

```csharp
using WSC.DataAccess.Attributes;
using WSC.DataAccess.Constants;
using WSC.DataAccess.Core;
using WSC.DataAccess.Repository;

/// <summary>
/// Order Repository - Load DAO005.xml
/// </summary>
[SqlMapFile(SqlMapFiles.DAO005)]  // ✨ Magic here!
public class OrderRepository : SimpleSqlMapRepository<Order>
{
    public OrderRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory)
    {
        // DAO005.xml tự động được load!
    }

    // Methods như bình thường
    public async Task<IEnumerable<Order>> GetAllAsync()
    {
        return await QueryListAsync("Order.GetAll");
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await QuerySingleAsync("Order.GetById", new { Id = id });
    }

    public async Task<int> CreateAsync(Order order)
    {
        return await ExecuteAsync("Order.Insert", order);
    }
}
```

---

## 🚀 Sử Dụng

### Register DI

```csharp
services.AddWscDataAccess(connectionString);
services.AddScoped<OrderRepository>();
```

### Sử dụng

```csharp
public class OrderService
{
    private readonly OrderRepository _repo;

    public OrderService(OrderRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<Order>> GetOrders()
    {
        return await _repo.GetAllAsync();
    }
}
```

---

## 💡 IntelliSense Support

Khi gõ:

```csharp
[SqlMapFile(SqlMapFiles.
//                      ^
//                      Nhấn Ctrl+Space
```

**Tất cả files hiện ra**:
```
SqlMapFiles.DAO001
SqlMapFiles.DAO002
SqlMapFiles.DAO003
...
SqlMapFiles.DAO005  ← Chọn này!
...
SqlMapFiles.APPLICATION_MAP
SqlMapFiles.GENERIC_MAP
```

**Chỉ cần chọn!** ✨

---

## 📊 3 Patterns Available

| Pattern | Complexity | Lines | IntelliSense | Type-Safe | Recommended |
|---------|-----------|-------|--------------|-----------|-------------|
| **Hardcoded** | Simple | 7 | ❌ | ❌ | ❌ |
| **Constants** | Medium | 7 | ✅ | ✅ | ✅ (Good) |
| **Attribute** | Simplest | **4** | ✅ | ✅ | ⭐ **BEST!** |

---

## 🎯 Chọn Pattern Nào?

### Dùng **Attribute Pattern** (SimpleSqlMapRepository) khi:
- ✅ **Muốn đơn giản nhất** (recommended!)
- ✅ 1 repository = 1 file (hoặc vài files cố định)
- ✅ Không cần logic phức tạp để chọn file

```csharp
[SqlMapFile(SqlMapFiles.DAO005)]  // ← Đơn giản!
public class OrderRepository : SimpleSqlMapRepository<Order>
```

---

### Dùng **Constants Pattern** (ScopedSqlMapRepository) khi:
- Cần flexibility hơn
- Dynamic file selection
- Custom logic trong constructor

```csharp
private const string SQL_MAP_FILE = SqlMapFiles.DAO005;  // ← Flexible

public OrderRepository(IDbSessionFactory sf)
    : base(sf, SQL_MAP_FILE)
```

---

## 🔥 Nhiều Files với Attribute

```csharp
[SqlMapFiles(
    SqlMapFiles.DAO005,   // Order
    SqlMapFiles.DAO010,   // Customer
    SqlMapFiles.DAO015    // Product
)]
public class ReportRepository : SimpleSqlMapRepository<dynamic>
{
    public ReportRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory)
    {
        // Load 3 files tự động!
    }

    public async Task<IEnumerable<dynamic>> GetOrders()
    {
        return await QueryListAsync("Order.GetAll");
    }

    public async Task<IEnumerable<dynamic>> GetCustomers()
    {
        return await QueryListAsync("Customer.GetAll");
    }
}
```

---

## ✅ Migration Guide

### Từ Hardcoded → Attribute

**Before**:
```csharp
public class OrderRepository : ScopedSqlMapRepository<Order>
{
    private const string SQL_MAP_FILE = "SqlMaps/DAO005.xml";

    public OrderRepository(IDbSessionFactory sf)
        : base(sf, SQL_MAP_FILE) { }
}
```

**After**:
```csharp
[SqlMapFile(SqlMapFiles.DAO005)]  // ← Thêm attribute
public class OrderRepository : SimpleSqlMapRepository<Order>  // ← Đổi base class
{
    public OrderRepository(IDbSessionFactory sf)
        : base(sf) { }  // ← Bỏ SQL_MAP_FILE parameter

    // Xóa: private const string SQL_MAP_FILE = ...
}
```

**Saved**: 3 dòng code! ✨

---

## 📚 Tóm Tắt

### ✨ Simplest Way:

1. **Add attribute** với file cần dùng
2. **Extend SimpleSqlMapRepository**
3. **Done!**

```csharp
[SqlMapFile(SqlMapFiles.DAO005)]  // 1. Attribute
public class OrderRepository : SimpleSqlMapRepository<Order>  // 2. Extend
{
    public OrderRepository(IDbSessionFactory sf) : base(sf) { }  // 3. Done!
}
```

**Just 4 lines!** 🎉

---

### Available Attributes:

- `[SqlMapFile(path)]` - 1 file
- `[SqlMapFiles(path1, path2, ...)]` - Nhiều files

### Available Constants:

- `SqlMapFiles.DAO001` → `DAO020`
- `SqlMapFiles.APPLICATION_MAP`
- `SqlMapFiles.GENERIC_MAP`
- `SqlMapFiles.PRODUCT_MAP`
- `SqlMapFiles.CUSTOMER_MAP`
- `SqlMapFiles.ORDER_MAP`

---

✅ **Đơn giản nhất rồi!** Chỉ cần `[SqlMapFile(SqlMapFiles.DAO005)]` 🚀
