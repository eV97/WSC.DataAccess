# Troubleshooting Guide - WSC.DataAccess

Hướng dẫn khắc phục các vấn đề thường gặp khi sử dụng WSC.DataAccess.

## Mục lục

1. [Vấn đề Connection](#1-vấn-đề-connection)
2. [Vấn đề SQL Mapping](#2-vấn-đề-sql-mapping)
3. [Vấn đề Transaction](#3-vấn-đề-transaction)
4. [Vấn đề Performance](#4-vấn-đề-performance)
5. [Vấn đề Dependency Injection](#5-vấn-đề-dependency-injection)
6. [Các lỗi thường gặp](#6-các-lỗi-thường-gặp)

---

## 1. Vấn đề Connection

### Lỗi: "Connection string cannot be null or empty"

**Nguyên nhân:**
- Connection string không được cấu hình đúng trong appsettings.json
- Connection string bị null khi đăng ký services

**Giải pháp:**

```csharp
// ❌ SAI - Có thể null
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddWscDataAccess(connectionString);

// ✅ ĐÚNG - Throw exception nếu null
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");
builder.Services.AddWscDataAccess(connectionString);
```

### Lỗi: "A network-related or instance-specific error occurred"

**Nguyên nhân:**
- SQL Server không chạy
- Firewall block connection
- Connection string sai

**Giải pháp:**

1. Kiểm tra SQL Server đang chạy:
```bash
# Windows
services.msc -> tìm SQL Server

# Linux
sudo systemctl status mssql-server
```

2. Test connection string:
```csharp
using Microsoft.Data.SqlClient;

var connectionString = "Server=localhost;Database=MyDb;...";
using (var connection = new SqlConnection(connectionString))
{
    connection.Open();
    Console.WriteLine("Connection successful!");
}
```

3. Thêm `TrustServerCertificate=True` nếu dùng local:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyDb;User Id=sa;Password=Pass123;TrustServerCertificate=True;"
  }
}
```

### Lỗi: "Login failed for user"

**Nguyên nhân:**
- Username/password sai
- User không có quyền truy cập database

**Giải pháp:**

```sql
-- Tạo login nếu chưa có
CREATE LOGIN myuser WITH PASSWORD = 'Pass123!';

-- Tạo user trong database
USE MyDatabase;
CREATE USER myuser FOR LOGIN myuser;

-- Cấp quyền
ALTER ROLE db_datareader ADD MEMBER myuser;
ALTER ROLE db_datawriter ADD MEMBER myuser;
```

---

## 2. Vấn đề SQL Mapping

### Lỗi: "SQL statement 'Product.GetById' not found in configuration"

**Nguyên nhân:**
- XML file không được load
- Statement ID không đúng
- File path sai

**Giải pháp:**

1. Kiểm tra file được đăng ký:
```csharp
builder.Services.AddWscDataAccess(connectionString, options =>
{
    // Đảm bảo path đúng
    options.AddSqlMapFile("SqlMaps/ProductMap.xml");
});
```

2. Kiểm tra file được copy vào output:
```xml
<!-- Trong .csproj -->
<ItemGroup>
  <None Update="SqlMaps\**\*.xml">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

3. Kiểm tra statement ID trong XML:
```xml
<!-- ID phải match exactly -->
<select id="Product.GetById" resultType="MyApp.Models.Product">
  SELECT * FROM Products WHERE Id = @Id
</select>
```

### Lỗi: "The CommandText property has not been properly initialized"

**Nguyên nhân:**
- XML statement rỗng hoặc chỉ có whitespace

**Giải pháp:**

```xml
<!-- ❌ SAI - Empty statement -->
<select id="Product.GetAll" resultType="MyApp.Models.Product">
</select>

<!-- ✅ ĐÚNG -->
<select id="Product.GetAll" resultType="MyApp.Models.Product">
  SELECT * FROM Products
</select>
```

### Lỗi: "Could not find type specified in resultType"

**Nguyên nhân:**
- Type name không đúng hoặc không có namespace đầy đủ

**Giải pháp:**

```xml
<!-- ❌ SAI - Thiếu namespace -->
<select id="Product.GetAll" resultType="Product">

<!-- ❌ SAI - Assembly qualified name không cần thiết -->
<select id="Product.GetAll" resultType="MyApp.Models.Product, MyApp">

<!-- ✅ ĐÚNG - Full namespace -->
<select id="Product.GetAll" resultType="MyApp.Models.Product">
```

---

## 3. Vấn đề Transaction

### Lỗi: "Transaction already started"

**Nguyên nhân:**
- Gọi BeginTransaction() hai lần trên cùng một session

**Giải pháp:**

```csharp
// ❌ SAI
using var session = SessionFactory.OpenSession();
session.BeginTransaction();
session.BeginTransaction(); // Error!

// ✅ ĐÚNG
using var session = SessionFactory.OpenSession();
session.BeginTransaction();
// ... operations ...
session.Commit();
```

### Lỗi: "No transaction to commit"

**Nguyên nhân:**
- Gọi Commit() mà không gọi BeginTransaction() trước

**Giải pháp:**

```csharp
// ✅ ĐÚNG
using var session = SessionFactory.OpenSession();
session.BeginTransaction();
try
{
    // ... operations ...
    session.Commit();
}
catch
{
    session.Rollback();
    throw;
}
```

### Deadlock

**Nguyên nhân:**
- Hai transactions lock nhau

**Giải pháp:**

1. Luôn lock theo thứ tự nhất quán:
```csharp
// ✅ ĐÚNG - Luôn lock Products trước, Orders sau
await UpdateProductAsync(productId);
await UpdateOrderAsync(orderId);
```

2. Giữ transactions ngắn:
```csharp
// ❌ SAI - Transaction quá dài
using var session = SessionFactory.OpenSession();
session.BeginTransaction();
// ... many operations ...
// ... business logic ...
// ... external API calls ... <- Tránh!
session.Commit();

// ✅ ĐÚNG - Transaction ngắn gọn
using var session = SessionFactory.OpenSession();
session.BeginTransaction();
// Chỉ database operations
await InsertOrderAsync(session, order);
await UpdateInventoryAsync(session, productId);
session.Commit();
```

---

## 4. Vấn đề Performance

### Query chậm

**Chẩn đoán:**

1. Bật SQL Profiler hoặc Extended Events
2. Kiểm tra execution plan
3. Tìm missing indexes

**Giải pháp:**

1. Thêm indexes:
```sql
-- Tìm missing indexes
SELECT
    migs.avg_user_impact,
    migs.avg_total_user_cost,
    mid.statement,
    mid.equality_columns,
    mid.inequality_columns,
    mid.included_columns
FROM sys.dm_db_missing_index_group_stats AS migs
INNER JOIN sys.dm_db_missing_index_groups AS mig
    ON migs.group_handle = mig.index_group_handle
INNER JOIN sys.dm_db_missing_index_details AS mid
    ON mig.index_handle = mid.index_handle
ORDER BY migs.avg_user_impact DESC;

-- Tạo index
CREATE INDEX IX_Products_Category ON Products(Category);
```

2. Optimize query:
```csharp
// ❌ SAI - N+1 query problem
var orders = await GetAllOrdersAsync();
foreach (var order in orders)
{
    var customer = await GetCustomerAsync(order.CustomerId); // N queries!
}

// ✅ ĐÚNG - JOIN trong SQL
var sql = @"
    SELECT o.*, c.*
    FROM Orders o
    INNER JOIN Customers c ON o.CustomerId = c.Id";
var results = await session.Connection.QueryAsync<Order, Customer, Order>(
    sql,
    (order, customer) => {
        order.Customer = customer;
        return order;
    },
    splitOn: "Id");
```

### Memory leak

**Nguyên nhân:**
- Không dispose DbSession
- Connection không được đóng

**Giải pháp:**

```csharp
// ❌ SAI - Không dispose
var session = SessionFactory.OpenSession();
// ... operations ...
// Session không bao giờ được dispose!

// ✅ ĐÚNG - Sử dụng using
using var session = SessionFactory.OpenSession();
// ... operations ...
// Tự động dispose khi ra khỏi scope
```

### Connection pool exhausted

**Nguyên nhân:**
- Quá nhiều connections không được close
- Connection pool size quá nhỏ

**Giải pháp:**

1. Đảm bảo dispose sessions:
```csharp
// ✅ ĐÚNG
using var session = SessionFactory.OpenSession();
```

2. Tăng pool size (nếu cần):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyDb;Min Pool Size=5;Max Pool Size=200;..."
  }
}
```

---

## 5. Vấn đề Dependency Injection

### Lỗi: "Unable to resolve service for type IDbSessionFactory"

**Nguyên nhân:**
- Chưa đăng ký WSC Data Access services

**Giải pháp:**

```csharp
// Đảm bảo gọi AddWscDataAccess
builder.Services.AddWscDataAccess(connectionString);
```

### Lỗi: "Cannot consume scoped service from singleton"

**Nguyên nhân:**
- Repository được inject vào singleton service

**Giải pháp:**

```csharp
// ❌ SAI - Repository trong Singleton
builder.Services.AddSingleton<MySingletonService>(); // Có inject Repository

// ✅ ĐÚNG - Dùng Scoped hoặc Transient
builder.Services.AddScoped<MyService>();

// HOẶC inject IDbSessionFactory thay vì Repository
public class MySingletonService
{
    private readonly IDbSessionFactory _sessionFactory;

    public MySingletonService(IDbSessionFactory sessionFactory)
    {
        _sessionFactory = sessionFactory;
    }
}
```

---

## 6. Các lỗi thường gặp

### ArgumentException: "Statement ID cannot be null or empty"

**Giải pháp:**
```csharp
// Đảm bảo statement ID không null
await QuerySingleAsync("Product.GetById", new { Id = id });
```

### InvalidOperationException: "Sequence contains no elements"

**Nguyên nhân:**
- Query không trả về kết quả nhưng dùng `First()` hoặc `Single()`

**Giải pháp:**
```csharp
// ❌ SAI - Throw exception nếu không tìm thấy
var product = await QuerySingleAsync("Product.GetById", ...);
var firstProduct = await QueryAsync(...).First();

// ✅ ĐÚNG - Trả về null nếu không tìm thấy
var product = await QuerySingleAsync("Product.GetById", ...); // Returns null
var firstProduct = await QueryAsync(...).FirstOrDefault();
```

### SqlException: "Invalid column name"

**Nguyên nhân:**
- Column trong SQL không tồn tại trong bảng
- Mapping không đúng

**Giải pháp:**

1. Kiểm tra table schema:
```sql
SELECT COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Products';
```

2. Kiểm tra property names match với columns:
```csharp
public class Product
{
    public int Id { get; set; }
    public string ProductName { get; set; } // Phải match với column name
}
```

### Timeout Expired

**Giải pháp:**

1. Tăng timeout trong XML:
```xml
<select id="Product.HeavyQuery" timeout="120">
  SELECT * FROM Products
  -- Complex query
</select>
```

2. Hoặc trong connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "...;Connect Timeout=60;..."
  }
}
```

---

## Debug Tips

### Enable SQL Logging

```csharp
// Thêm vào Program.cs
builder.Logging.AddFilter("Microsoft.Data.SqlClient", LogLevel.Information);
```

### Test Connection

```csharp
public async Task<bool> TestConnectionAsync()
{
    try
    {
        using var session = _sessionFactory.OpenSession();
        await session.Connection.ExecuteScalarAsync<int>("SELECT 1");
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Connection test failed");
        return false;
    }
}
```

### Verify SQL Map Loaded

```csharp
public void VerifySqlMaps(SqlMapConfig config)
{
    var statements = config.GetAllStatements();
    foreach (var statement in statements)
    {
        Console.WriteLine($"Loaded: {statement.Key}");
    }
}
```

---

## Liên hệ Support

Nếu bạn gặp vấn đề không được liệt kê ở đây:

1. Kiểm tra [GitHub Issues](https://github.com/eV97/WSC.DataAccess/issues)
2. Tạo issue mới với:
   - Mô tả vấn đề
   - Code snippet
   - Stack trace
   - Environment info (.NET version, SQL Server version)

---

**Chúc bạn khắc phục thành công!** 🔧
