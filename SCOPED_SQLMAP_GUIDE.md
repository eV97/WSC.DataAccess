# Scoped SQL Map Guide

Hướng dẫn sử dụng SQL map files riêng biệt cho từng service/repository.

## 🎯 Vấn Đề

Khi load **TẤT CẢ** SQL map files vào một SqlMapConfig global:

```csharp
services.AddWscDataAccess(connectionString, options =>
{
    options.AddSqlMapFile("SqlMaps/DAO001.xml");  // Service A
    options.AddSqlMapFile("SqlMaps/DAO005.xml");  // Service B
    options.AddSqlMapFile("SqlMaps/DAO010.xml");  // Service C
    // ... load hết tất cả files
});
```

**Vấn đề**:
- ❌ Nếu DAO010.xml có lỗi → Service A và B cũng bị ảnh hưởng
- ❌ Statement ID có thể bị conflict giữa các files
- ❌ Load nhiều files không cần thiết → performance kém
- ❌ Khó maintain khi project lớn

---

## ✅ Giải Pháp: Scoped SQL Map

Mỗi service/repository **CHỈ load SQL map file riêng** của nó.

### Ví dụ:
- **Service A** → chỉ load `DAO005.xml`
- **Service B** → chỉ load `DAO010.xml`
- **Service C** → chỉ load `DAO015.xml`

**Lợi ích**:
- ✅ Isolation hoàn toàn giữa các services
- ✅ Lỗi ở file này không ảnh hưởng file khác
- ✅ Không lo conflict statement IDs
- ✅ Performance tốt hơn (chỉ load cần thiết)
- ✅ Dễ maintain và test

---

## 📋 Cách Sử Dụng

### 1. Tạo Repository với SQL Map File Riêng

Extend từ `ScopedSqlMapRepository<T>` và chỉ định file:

```csharp
using WSC.DataAccess.Repository;
using WSC.DataAccess.Core;
using WSC.DataAccess.Mapping;

public class OrderRepository : ScopedSqlMapRepository<Order>
{
    // Chỉ định SQL map file riêng
    private const string SQL_MAP_FILE = "SqlMaps/DAO005.xml";

    public OrderRepository(
        IDbSessionFactory sessionFactory,
        ILogger<SqlMapConfig>? loggerConfig = null,
        ILogger<SqlMapper>? loggerMapper = null,
        ILogger<OrderRepository>? logger = null)
        : base(sessionFactory, SQL_MAP_FILE, loggerConfig, loggerMapper, logger)
    {
        // Repository này CHỈ load DAO005.xml
        // Hoàn toàn độc lập với các SQL map files khác
    }

    // Sử dụng statements từ DAO005.xml
    public async Task<IEnumerable<Order>> GetAllOrdersAsync()
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
}
```

---

### 2. Repository với Nhiều SQL Map Files

Nếu service cần load nhiều files:

```csharp
public class ComplexRepository : ScopedSqlMapRepository<dynamic>
{
    private static readonly string[] SQL_MAP_FILES = new[]
    {
        "SqlMaps/DAO005.xml",
        "SqlMaps/DAO006.xml"
    };

    public ComplexRepository(
        IDbSessionFactory sessionFactory,
        ILogger<SqlMapConfig>? loggerConfig = null,
        ILogger<SqlMapper>? loggerMapper = null)
        : base(sessionFactory, SQL_MAP_FILES, loggerConfig, loggerMapper)
    {
        // Repository này load 2 files: DAO005.xml và DAO006.xml
    }
}
```

---

### 3. Register trong DI Container

**Không cần** đăng ký SQL map files trong `AddWscDataAccess`:

```csharp
// Program.cs hoặc Startup.cs

services.AddWscDataAccess(connectionString);  // Không cần options.AddSqlMapFile()

// Đăng ký repositories
services.AddScoped<OrderRepository>();
services.AddScoped<CustomerRepository>();
services.AddScoped<ProductRepository>();
```

Mỗi repository sẽ tự load SQL map file riêng của nó!

---

### 4. Sử dụng Repository

```csharp
public class OrderService
{
    private readonly OrderRepository _orderRepo;

    public OrderService(OrderRepository orderRepo)
    {
        _orderRepo = orderRepo;
    }

    public async Task<IEnumerable<Order>> GetAllOrders()
    {
        // OrderRepository chỉ load DAO005.xml
        // Không bị ảnh hưởng bởi DAO010.xml hoặc files khác
        return await _orderRepo.GetAllOrdersAsync();
    }
}
```

---

## 🆚 So Sánh: Global vs Scoped

### Global SqlMapConfig (Cách Cũ)

```csharp
// Đăng ký tất cả files
services.AddWscDataAccess(connectionString, options =>
{
    options.AddSqlMapFile("SqlMaps/DAO005.xml");  // Order
    options.AddSqlMapFile("SqlMaps/DAO010.xml");  // Customer
    options.AddSqlMapFile("SqlMaps/DAO015.xml");  // Product
});

// Tất cả repositories dùng chung SqlMapConfig
services.AddScoped<OrderRepository>();    // có DAO005 + DAO010 + DAO015
services.AddScoped<CustomerRepository>(); // có DAO005 + DAO010 + DAO015
services.AddScoped<ProductRepository>();  // có DAO005 + DAO010 + DAO015
```

**Vấn đề**:
- Mỗi repository load HẾT tất cả files
- Conflict statement IDs
- Lỗi 1 file ảnh hưởng tất cả

---

### Scoped SqlMapConfig (Cách Mới) ✅

```csharp
// KHÔNG cần đăng ký files
services.AddWscDataAccess(connectionString);

// Mỗi repository tự load file riêng
services.AddScoped<OrderRepository>();    // CHỈ load DAO005.xml
services.AddScoped<CustomerRepository>(); // CHỈ load DAO010.xml
services.AddScoped<ProductRepository>();  // CHỈ load DAO015.xml
```

**Lợi ích**:
- ✅ Isolation hoàn toàn
- ✅ Không conflict
- ✅ Lỗi không lan truyền
- ✅ Performance tốt hơn

---

## 📝 Ví Dụ Thực Tế

### Scenario: 3 Services

```
Project Structure:
├── Services/
│   ├── OrderService       → Dùng DAO005.xml
│   ├── CustomerService    → Dùng DAO010.xml
│   └── ProductService     → Dùng DAO015.xml
└── SqlMaps/
    ├── DAO005.xml (Order statements)
    ├── DAO010.xml (Customer statements)
    └── DAO015.xml (Product statements)
```

#### OrderRepository.cs
```csharp
public class OrderRepository : ScopedSqlMapRepository<Order>
{
    private const string SQL_MAP_FILE = "SqlMaps/DAO005.xml";

    public OrderRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory, SQL_MAP_FILE)
    {
    }
}
```

#### CustomerRepository.cs
```csharp
public class CustomerRepository : ScopedSqlMapRepository<Customer>
{
    private const string SQL_MAP_FILE = "SqlMaps/DAO010.xml";

    public CustomerRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory, SQL_MAP_FILE)
    {
    }
}
```

#### ProductRepository.cs
```csharp
public class ProductRepository : ScopedSqlMapRepository<Product>
{
    private const string SQL_MAP_FILE = "SqlMaps/DAO015.xml";

    public ProductRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory, SQL_MAP_FILE)
    {
    }
}
```

**Kết quả**:
- DAO010.xml có lỗi → CHỈ CustomerService bị lỗi
- OrderService và ProductService vẫn hoạt động bình thường ✅

---

## 🔧 Advanced: SqlMapConfigBuilder

Nếu cần tạo SqlMapConfig manually:

```csharp
using WSC.DataAccess.Mapping;

// Tạo từ 1 file
var config = SqlMapConfigBuilder.FromFile("SqlMaps/DAO005.xml", logger);

// Tạo từ nhiều files
var config = SqlMapConfigBuilder.FromFiles(logger,
    "SqlMaps/DAO005.xml",
    "SqlMaps/DAO006.xml");

// Hoặc dùng builder pattern
var config = new SqlMapConfigBuilder(logger)
    .AddSqlMapFile("SqlMaps/DAO005.xml")
    .AddSqlMapFile("SqlMaps/DAO006.xml")
    .Build();

// Tạo SqlMapper
var sqlMapper = new SqlMapper(config, mapperLogger);
```

---

## 📊 Logging

Logs sẽ cho thấy file nào được load:

```log
[INF] WSC.DataAccess.Mapping.SqlMapConfig: Loading SQL map file: SqlMaps/DAO005.xml
[DBG] WSC.DataAccess.Mapping.SqlMapConfig: Loaded SELECT statement: Order.GetAll
[DBG] WSC.DataAccess.Mapping.SqlMapConfig: Loaded SELECT statement: Order.GetById
[INF] WSC.DataAccess.Mapping.SqlMapConfig: Successfully loaded SQL map file: SqlMaps/DAO005.xml. Total statements: 5 (SELECT: 3, INSERT: 1, UPDATE: 1, DELETE: 0, PROCEDURE: 0)
```

Mỗi repository log riêng file của nó!

---

## ✅ Best Practices

1. **Một repository = Một SQL map file (hoặc vài files liên quan)**
   ```csharp
   private const string SQL_MAP_FILE = "SqlMaps/DAO005.xml";
   ```

2. **Đặt tên file rõ ràng**
   ```
   DAO005_Order.xml      ← Rõ ràng là cho Order
   DAO010_Customer.xml   ← Rõ ràng là cho Customer
   ```

3. **Statement ID có prefix**
   ```xml
   <select id="Order.GetAll">...</select>
   <select id="Customer.GetAll">...</select>
   ```
   Tránh conflict nếu sau này cần merge files.

4. **Test riêng từng repository**
   ```csharp
   // Test chỉ OrderRepository với DAO005.xml
   var repo = new OrderRepository(sessionFactory);
   var orders = await repo.GetAllOrdersAsync();
   ```

5. **Logging để tracking**
   - Inject logger để biết file nào được load
   - Debug dễ dàng khi có lỗi

---

## 🆚 Khi Nào Dùng Cách Nào?

### Dùng **Global SqlMapConfig** khi:
- ✅ Project nhỏ (< 10 SQL map files)
- ✅ Tất cả statements được quản lý tập trung
- ✅ Không lo conflict statement IDs
- ✅ Muốn đơn giản

### Dùng **Scoped SqlMapConfig** khi:
- ✅ Project lớn (nhiều services/repositories)
- ✅ Mỗi service/team quản lý SQL map riêng
- ✅ Cần isolation giữa các modules
- ✅ Muốn tránh conflict statement IDs
- ✅ Performance quan trọng
- ✅ Muốn test riêng từng service

---

## 📚 Tóm Tắt

| Đặc điểm | Global SqlMapConfig | Scoped SqlMapConfig |
|----------|---------------------|---------------------|
| Load files | Tất cả cùng lúc | Từng file riêng biệt |
| Isolation | ❌ Không | ✅ Có |
| Conflict risk | ⚠️ Cao | ✅ Không |
| Error propagation | ❌ Lan truyền | ✅ Cô lập |
| Performance | ⚠️ Load nhiều | ✅ Load ít |
| Maintainability | ⚠️ Khó khi lớn | ✅ Dễ |
| Use case | Project nhỏ | Project lớn |

---

## 🎯 Migration từ Global → Scoped

### Bước 1: Tạo Scoped Repositories

```csharp
// Trước (Global)
public class OrderRepository : SqlMapRepository<Order>
{
    public OrderRepository(IDbSessionFactory sf, SqlMapper mapper)
        : base(sf, mapper) { }
}

// Sau (Scoped)
public class OrderRepository : ScopedSqlMapRepository<Order>
{
    private const string SQL_MAP_FILE = "SqlMaps/DAO005.xml";

    public OrderRepository(IDbSessionFactory sf)
        : base(sf, SQL_MAP_FILE) { }
}
```

### Bước 2: Update DI Registration

```csharp
// Trước
services.AddWscDataAccess(connectionString, options =>
{
    options.AddSqlMapFile("SqlMaps/DAO005.xml");
});

// Sau
services.AddWscDataAccess(connectionString);
// Repository tự load file riêng
```

### Bước 3: Test

```bash
dotnet test
```

Done! ✅

---

✅ **Happy Coding với Scoped SQL Map!** 🎉
