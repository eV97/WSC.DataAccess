# SQL Map Constants Guide

Hướng dẫn sử dụng constants cho SQL map files - Tránh hardcoded strings!

## 🎯 Vấn Đề với Hardcoded Strings

### Trước (Hardcoded - Dễ sai):

```csharp
public class OrderRepository : ScopedSqlMapRepository<Order>
{
    // ❌ Hardcoded string - dễ typo, không có IntelliSense
    private const string SQL_MAP_FILE = "SqlMaps/DAO005.xml";
    //                                   ^^^^^^^^^^^^^^^
    //                                   Có thể gõ nhầm: "SqlMap/DAO005.xml"
    //                                                    "SqlMaps/DA005.xml"
    //                                                    "SqlMaps/DAO005.XML"
}
```

**Vấn đề**:
- ❌ Dễ typo (gõ nhầm)
- ❌ Không có IntelliSense
- ❌ Khó refactor (rename file phải sửa nhiều chỗ)
- ❌ Không type-safe
- ❌ Không biết file nào available

---

## ✅ Giải Pháp: SqlMapFiles Constants

### Bây Giờ (Constants - An toàn):

```csharp
using WSC.DataAccess.Constants;

public class OrderRepository : ScopedSqlMapRepository<Order>
{
    // ✅ Sử dụng constant - Type-safe, có IntelliSense
    private const string SQL_MAP_FILE = SqlMapFiles.DAO005;
    //                                  ^^^^^^^^^^^^^^^^^
    //                                  IntelliSense hiện tất cả options!
}
```

**Lợi ích**:
- ✅ IntelliSense support (gõ `SqlMapFiles.` và nhìn tất cả options)
- ✅ Type-safe (compile-time checking)
- ✅ Không typo
- ✅ Dễ refactor
- ✅ Tự document (XML comments)

---

## 📋 Available Constants

### DAO Files (DAO001 - DAO020)

```csharp
using WSC.DataAccess.Constants;

// DAO files
SqlMapFiles.DAO001  // "SqlMaps/DAO001.xml"
SqlMapFiles.DAO002  // "SqlMaps/DAO002.xml"
SqlMapFiles.DAO003  // "SqlMaps/DAO003.xml"
SqlMapFiles.DAO004  // "SqlMaps/DAO004.xml"
SqlMapFiles.DAO005  // "SqlMaps/DAO005.xml" - Order management
SqlMapFiles.DAO006  // "SqlMaps/DAO006.xml"
SqlMapFiles.DAO007  // "SqlMaps/DAO007.xml"
SqlMapFiles.DAO008  // "SqlMaps/DAO008.xml"
SqlMapFiles.DAO009  // "SqlMaps/DAO009.xml"
SqlMapFiles.DAO010  // "SqlMaps/DAO010.xml" - Customer management
SqlMapFiles.DAO011  // "SqlMaps/DAO011.xml"
SqlMapFiles.DAO012  // "SqlMaps/DAO012.xml"
SqlMapFiles.DAO013  // "SqlMaps/DAO013.xml"
SqlMapFiles.DAO014  // "SqlMaps/DAO014.xml"
SqlMapFiles.DAO015  // "SqlMaps/DAO015.xml" - Product management
SqlMapFiles.DAO016  // "SqlMaps/DAO016.xml"
SqlMapFiles.DAO017  // "SqlMaps/DAO017.xml"
SqlMapFiles.DAO018  // "SqlMaps/DAO018.xml"
SqlMapFiles.DAO019  // "SqlMaps/DAO019.xml"
SqlMapFiles.DAO020  // "SqlMaps/DAO020.xml" - Reporting
```

### Named Map Files

```csharp
// Named files
SqlMapFiles.APPLICATION_MAP  // "SqlMaps/ApplicationMap.xml"
SqlMapFiles.GENERIC_MAP      // "SqlMaps/GenericMap.xml"
SqlMapFiles.PRODUCT_MAP      // "SqlMaps/ProductMap.xml"
SqlMapFiles.USER_MAP         // "SqlMaps/UserMap.xml"
SqlMapFiles.CUSTOMER_MAP     // "SqlMaps/CustomerMap.xml"
SqlMapFiles.ORDER_MAP        // "SqlMaps/OrderMap.xml"
```

---

## 🚀 Cách Sử Dụng

### 1. Trong Repository

```csharp
using WSC.DataAccess.Constants;
using WSC.DataAccess.Repository;

public class OrderRepository : ScopedSqlMapRepository<Order>
{
    // ✨ Chỉ cần: SqlMapFiles.DAO005
    private const string SQL_MAP_FILE = SqlMapFiles.DAO005;

    public OrderRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory, SQL_MAP_FILE)
    {
    }
}
```

### 2. Nhiều Files

```csharp
public class ComplexRepository : ScopedSqlMapRepository<dynamic>
{
    // ✨ Array of constants
    private static readonly string[] SQL_MAP_FILES = new[]
    {
        SqlMapFiles.DAO005,  // Order
        SqlMapFiles.DAO010,  // Customer
        SqlMapFiles.DAO015   // Product
    };

    public ComplexRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory, SQL_MAP_FILES)
    {
    }
}
```

### 3. Check File Exists

```csharp
var basePath = AppContext.BaseDirectory;

// Check nếu file tồn tại
if (SqlMapFiles.Exists(basePath, SqlMapFiles.DAO005))
{
    Console.WriteLine("DAO005.xml exists!");
}

// Get full path
var fullPath = SqlMapFiles.GetFullPath(basePath, SqlMapFiles.DAO005);
// => "/app/SqlMaps/DAO005.xml"
```

### 4. List All Available Files

```csharp
// Lấy tất cả DAO files
var daoFiles = SqlMapFiles.GetAllDaoFiles();
foreach (var file in daoFiles)
{
    Console.WriteLine(file);
}
// Output:
// SqlMaps/DAO001.xml
// SqlMaps/DAO002.xml
// ...
// SqlMaps/DAO020.xml

// Lấy tất cả named map files
var namedFiles = SqlMapFiles.GetAllNamedMapFiles();
foreach (var file in namedFiles)
{
    Console.WriteLine(file);
}
// Output:
// SqlMaps/ApplicationMap.xml
// SqlMaps/GenericMap.xml
// ...
```

---

## 💡 IntelliSense Demo

Khi gõ code:

```csharp
private const string SQL_MAP_FILE = SqlMapFiles.
//                                              ^
//                                              Nhấn Ctrl+Space
```

**IntelliSense sẽ hiện**:
```
SqlMapFiles
├── DAO001          "SqlMaps/DAO001.xml"
├── DAO002          "SqlMaps/DAO002.xml"
├── DAO003          "SqlMaps/DAO003.xml"
├── DAO004          "SqlMaps/DAO004.xml"
├── DAO005          "SqlMaps/DAO005.xml" - Order management
├── ...
├── DAO020          "SqlMaps/DAO020.xml" - Reporting
├── APPLICATION_MAP "SqlMaps/ApplicationMap.xml"
├── GENERIC_MAP     "SqlMaps/GenericMap.xml"
├── PRODUCT_MAP     "SqlMaps/ProductMap.xml"
└── ...
```

Chỉ cần chọn! ✨

---

## 📝 Examples

### Example 1: Order Repository

```csharp
using WSC.DataAccess.Constants;

public class OrderRepository : ScopedSqlMapRepository<Order>
{
    private const string SQL_MAP_FILE = SqlMapFiles.DAO005;
    //                                  ^^^^^^^^^^^^^^^^^^^^
    //                                  IntelliSense support!

    public OrderRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory, SQL_MAP_FILE)
    {
    }

    public async Task<IEnumerable<Order>> GetAllAsync()
    {
        return await QueryListAsync("Order.GetAll");
    }
}
```

### Example 2: Customer Repository

```csharp
public class CustomerRepository : ScopedSqlMapRepository<Customer>
{
    private const string SQL_MAP_FILE = SqlMapFiles.DAO010;

    public CustomerRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory, SQL_MAP_FILE)
    {
    }
}
```

### Example 3: Product Repository

```csharp
public class ProductRepository : ScopedSqlMapRepository<Product>
{
    private const string SQL_MAP_FILE = SqlMapFiles.DAO015;

    public ProductRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory, SQL_MAP_FILE)
    {
    }
}
```

### Example 4: Multi-File Repository

```csharp
public class ReportRepository : ScopedSqlMapRepository<dynamic>
{
    // Sử dụng nhiều files với constants
    private static readonly string[] SQL_MAP_FILES = new[]
    {
        SqlMapFiles.DAO005,  // Orders
        SqlMapFiles.DAO010,  // Customers
        SqlMapFiles.DAO015   // Products
    };

    public ReportRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory, SQL_MAP_FILES)
    {
    }
}
```

---

## 🔧 Thêm Constants Mới

### Bước 1: Mở SqlMapFiles.cs

```csharp
// File: src/WSC.DataAccess/Constants/SqlMapFiles.cs

public static class SqlMapFiles
{
    // Thêm constant mới ở đây

    /// <summary>DAO021.xml - Invoice management</summary>
    public const string DAO021 = BASE_DIR + "/DAO021.xml";

    /// <summary>DAO022.xml - Payment processing</summary>
    public const string DAO022 = BASE_DIR + "/DAO022.xml";
}
```

### Bước 2: Sử dụng

```csharp
public class InvoiceRepository : ScopedSqlMapRepository<Invoice>
{
    private const string SQL_MAP_FILE = SqlMapFiles.DAO021;
    //                                  ^^^^^^^^^^^^^^^^^^^^
    //                                  IntelliSense tự động nhận!
}
```

---

## 📊 So Sánh

| Feature | Hardcoded String | **SqlMapFiles Constant** ✨ |
|---------|------------------|----------------------------|
| IntelliSense | ❌ Không | **✅ Có** |
| Type-safe | ❌ Không | **✅ Có** |
| Typo risk | ⚠️ Cao | **✅ Không** |
| Refactoring | ❌ Khó | **✅ Dễ** |
| Documentation | ❌ Không | **✅ XML comments** |
| Discover files | ❌ Không | **✅ Có (GetAllDaoFiles)** |

---

## 🎯 Best Practices

### ✅ DO: Sử dụng Constants

```csharp
// ✅ GOOD
private const string SQL_MAP_FILE = SqlMapFiles.DAO005;
```

### ❌ DON'T: Hardcoded Strings

```csharp
// ❌ BAD
private const string SQL_MAP_FILE = "SqlMaps/DAO005.xml";
```

### ✅ DO: Thêm XML Comments

```csharp
/// <summary>DAO025.xml - Inventory management</summary>
public const string DAO025 = BASE_DIR + "/DAO025.xml";
```

### ✅ DO: Group Related Files

```csharp
// Order-related files
private static readonly string[] ORDER_FILES = new[]
{
    SqlMapFiles.DAO005,  // Order
    SqlMapFiles.DAO006,  // OrderItem
    SqlMapFiles.DAO007   // OrderStatus
};
```

---

## 🔍 Helper Methods

### GetFullPath

```csharp
var basePath = AppContext.BaseDirectory;
var fullPath = SqlMapFiles.GetFullPath(basePath, SqlMapFiles.DAO005);
// => "/app/SqlMaps/DAO005.xml"
```

### Exists

```csharp
if (SqlMapFiles.Exists(basePath, SqlMapFiles.DAO005))
{
    // File exists, load it
}
else
{
    // File not found
    throw new FileNotFoundException();
}
```

### GetAllDaoFiles

```csharp
var allDaoFiles = SqlMapFiles.GetAllDaoFiles();
Console.WriteLine($"Total DAO files: {allDaoFiles.Length}");
// => Total DAO files: 20
```

### GetAllNamedMapFiles

```csharp
var namedFiles = SqlMapFiles.GetAllNamedMapFiles();
foreach (var file in namedFiles)
{
    Console.WriteLine($"- {file}");
}
// Output:
// - SqlMaps/ApplicationMap.xml
// - SqlMaps/GenericMap.xml
// - SqlMaps/ProductMap.xml
// ...
```

---

## 📚 Complete Example

```csharp
using WSC.DataAccess.Constants;
using WSC.DataAccess.Repository;

namespace MyApp.Repositories;

/// <summary>
/// Order Repository - Sử dụng SqlMapFiles.DAO005
/// </summary>
public class OrderRepository : ScopedSqlMapRepository<Order>
{
    // ✨ Constant - Type-safe, IntelliSense support
    private const string SQL_MAP_FILE = SqlMapFiles.DAO005;

    public OrderRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory, SQL_MAP_FILE)
    {
        // CHỈ load DAO005.xml
    }

    // CRUD operations
    public async Task<IEnumerable<Order>> GetAllAsync()
    {
        return await QueryListAsync("Order.GetAll");
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await QuerySingleAsync("Order.GetById", new { Id = id });
    }

    public async Task<int> InsertAsync(Order order)
    {
        return await ExecuteAsync("Order.Insert", order);
    }

    public async Task<int> UpdateAsync(Order order)
    {
        return await ExecuteAsync("Order.Update", order);
    }

    public async Task<int> DeleteAsync(int id)
    {
        return await ExecuteAsync("Order.Delete", new { Id = id });
    }
}

// Sử dụng
var orderRepo = services.GetRequiredService<OrderRepository>();
var orders = await orderRepo.GetAllAsync();
```

---

## ✅ Tóm Tắt

**Trước**:
```csharp
private const string SQL_MAP_FILE = "SqlMaps/DAO005.xml";
//                                   ^^^^^^^^^^^^^^^
//                                   Dễ sai, không IntelliSense
```

**Bây giờ**:
```csharp
private const string SQL_MAP_FILE = SqlMapFiles.DAO005;
//                                  ^^^^^^^^^^^^^^^^^
//                                  Type-safe, IntelliSense ✨
```

**Just type**: `SqlMapFiles.` và nhấn **Ctrl+Space** → Tất cả files hiện ra! 🎉

---

✅ **Happy Coding với SqlMapFiles Constants!**
