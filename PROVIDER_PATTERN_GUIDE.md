```markdown
# SQL Map Provider Pattern Guide

Hướng dẫn sử dụng Provider Pattern để khai báo SQL maps - Giống MrFu.Smartcheck!

## 🎯 Concept

**Provider Pattern**: Khai báo tất cả SQL map files TẬP TRUNG ở một chỗ (trong `Program.cs` hoặc `Startup.cs`), sau đó các repositories chỉ cần reference bằng **KEY**.

### Giống MrFu.Smartcheck

```csharp
// MrFu.Smartcheck pattern
services.AddSmartcheck(options =>
{
    options.ConfigureProviders(provider =>
    {
        provider.AddProvider("EmailProvider", config);
        provider.AddProvider("SmsProvider", config);
    });
});

// Service chỉ cần gọi provider.Get("EmailProvider")
```

### WSC.DataAccess Pattern (Giống vậy!)

```csharp
// Khai báo SQL maps TẬP TRUNG
services.AddWscDataAccess(connectionString, options =>
{
    options.ConfigureSqlMaps(provider =>
    {
        provider.AddFile("Order", SqlMapFiles.DAO005, "Order management");
        provider.AddFile("Customer", SqlMapFiles.DAO010, "Customer management");
        provider.AddFile("Product", SqlMapFiles.DAO015, "Product catalog");
    });
});

// Repository chỉ cần gọi provider.GetFilePath("Order")
```

---

## 📋 Cách Sử Dụng

### Bước 1: Khai Báo SQL Maps (Program.cs)

```csharp
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Constants;

var builder = WebApplication.CreateBuilder(args);

// Khai báo SQL Maps như Provider
builder.Services.AddWscDataAccess(connectionString, options =>
{
    options.ConfigureSqlMaps(provider =>
    {
        // Khai báo tập trung tất cả SQL map files
        provider.AddFile("Order", SqlMapFiles.DAO005, "Order management");
        provider.AddFile("Customer", SqlMapFiles.DAO010, "Customer data");
        provider.AddFile("Product", SqlMapFiles.DAO015, "Product catalog");
        provider.AddFile("Inventory", SqlMapFiles.DAO016, "Inventory tracking");
        provider.AddFile("Report", SqlMapFiles.DAO020, "Reporting queries");

        // Hoặc dùng named maps
        provider.AddFile("Application", SqlMapFiles.APPLICATION_MAP, "Application config");
        provider.AddFile("Generic", SqlMapFiles.GENERIC_MAP, "Generic utilities");
    });
});

// Đăng ký repositories
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<CustomerRepository>();
builder.Services.AddScoped<ProductRepository>();
```

**Lợi ích**: Tất cả SQL map files khai báo ở MỘT CHỖ, dễ quản lý!

---

### Bước 2: Tạo Repository Sử Dụng Provider

```csharp
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Core;
using WSC.DataAccess.Repository;

public class OrderRepository : ProviderBasedRepository<Order>
{
    // Chỉ cần KEY, không cần file path!
    private const string MAP_KEY = "Order";  // ← Key đã khai báo trong Program.cs

    public OrderRepository(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider)  // ← Inject provider
        : base(sessionFactory, provider, MAP_KEY)
    {
        // File path tự động lấy từ provider.GetFilePath("Order")
        // = SqlMapFiles.DAO005 (đã khai báo trong Program.cs)
    }

    // CRUD methods
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

### Bước 3: Sử Dụng Repository

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
        return await _orderRepo.GetAllAsync();
    }
}
```

**Done!** ✅

---

## 🆚 So Sánh Các Pattern

### Pattern 1: Hardcoded (Không khuyến khích)

```csharp
public class OrderRepository : ScopedSqlMapRepository<Order>
{
    private const string SQL_MAP_FILE = "SqlMaps/DAO005.xml";  // ❌ Hardcoded

    public OrderRepository(IDbSessionFactory sf)
        : base(sf, SQL_MAP_FILE) { }
}
```

**Vấn đề**:
- ❌ Mỗi repository phải khai báo lại file path
- ❌ Dễ sai, không tập trung
- ❌ Khó thay đổi file path

---

### Pattern 2: Constants (Tốt hơn)

```csharp
public class OrderRepository : ScopedSqlMapRepository<Order>
{
    private const string SQL_MAP_FILE = SqlMapFiles.DAO005;  // ✅ Constant

    public OrderRepository(IDbSessionFactory sf)
        : base(sf, SQL_MAP_FILE) { }
}
```

**Lợi ích**:
- ✅ IntelliSense support
- ⚠️ Vẫn phải khai báo ở mỗi repository

---

### Pattern 3: Attribute (Đơn giản)

```csharp
[SqlMapFile(SqlMapFiles.DAO005)]
public class OrderRepository : SimpleSqlMapRepository<Order>
{
    public OrderRepository(IDbSessionFactory sf) : base(sf) { }
}
```

**Lợi ích**:
- ✅ Rất đơn giản (4 dòng)
- ⚠️ File path vẫn ở trong repository

---

### Pattern 4: Provider (TẬP TRUNG - BEST!) ⭐

**Program.cs** (Khai báo TẬP TRUNG):
```csharp
options.ConfigureSqlMaps(provider =>
{
    provider.AddFile("Order", SqlMapFiles.DAO005);     // ← Khai báo ở đây
    provider.AddFile("Customer", SqlMapFiles.DAO010);  // ← Khai báo ở đây
    provider.AddFile("Product", SqlMapFiles.DAO015);   // ← Khai báo ở đây
});
```

**Repository** (Chỉ dùng KEY):
```csharp
public class OrderRepository : ProviderBasedRepository<Order>
{
    private const string MAP_KEY = "Order";  // ← Chỉ cần KEY!

    public OrderRepository(IDbSessionFactory sf, SqlMapProvider provider)
        : base(sf, provider, MAP_KEY) { }
}
```

**Lợi ích**:
- ✅ Tất cả SQL maps khai báo TẬP TRUNG
- ✅ Repository chỉ cần KEY
- ✅ Dễ quản lý, dễ thay đổi
- ✅ **Giống MrFu.Smartcheck pattern!**

---

## 📊 Comparison Table

| Feature | Hardcoded | Constants | Attribute | **Provider** ⭐ |
|---------|-----------|-----------|-----------|----------------|
| Centralized config | ❌ | ❌ | ❌ | **✅** |
| IntelliSense | ❌ | ✅ | ✅ | ✅ |
| Type-safe | ❌ | ✅ | ✅ | ✅ |
| Easy to change | ❌ | ⚠️ | ⚠️ | **✅** |
| Lines of code | 7 | 7 | 4 | **5** |
| Best for | ❌ | Small projects | Simple cases | **Enterprise!** |

---

## 💡 Complete Example

### Program.cs

```csharp
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Constants;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Khai báo SQL Maps như Provider (TẬP TRUNG)
builder.Services.AddWscDataAccess(connectionString, options =>
{
    options.ConfigureSqlMaps(provider =>
    {
        // ✨ Tất cả SQL maps khai báo ở đây!
        provider.AddFile("Order", SqlMapFiles.DAO005, "Order management");
        provider.AddFile("OrderItem", SqlMapFiles.DAO006, "Order items");
        provider.AddFile("Customer", SqlMapFiles.DAO010, "Customer data");
        provider.AddFile("Product", SqlMapFiles.DAO015, "Product catalog");
        provider.AddFile("Inventory", SqlMapFiles.DAO016, "Inventory tracking");
        provider.AddFile("Payment", SqlMapFiles.DAO017, "Payment processing");
        provider.AddFile("Shipping", SqlMapFiles.DAO018, "Shipping info");
        provider.AddFile("Report", SqlMapFiles.DAO020, "Reporting queries");
    });
});

// Đăng ký repositories
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<CustomerRepository>();
builder.Services.AddScoped<ProductRepository>();
builder.Services.AddScoped<ReportRepository>();

var app = builder.Build();
app.Run();
```

---

### OrderRepository.cs

```csharp
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Core;
using WSC.DataAccess.Repository;

public class OrderRepository : ProviderBasedRepository<Order>
{
    private const string MAP_KEY = "Order";  // ← Key trong provider

    public OrderRepository(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider)
        : base(sessionFactory, provider, MAP_KEY)
    {
    }

    public async Task<IEnumerable<Order>> GetAllAsync()
    {
        return await QueryListAsync("Order.GetAll");
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await QuerySingleAsync("Order.GetById", new { Id = id });
    }
}
```

---

### CustomerRepository.cs

```csharp
public class CustomerRepository : ProviderBasedRepository<Customer>
{
    private const string MAP_KEY = "Customer";  // ← Key trong provider

    public CustomerRepository(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider)
        : base(sessionFactory, provider, MAP_KEY)
    {
    }

    public async Task<IEnumerable<Customer>> GetAllAsync()
    {
        return await QueryListAsync("Customer.GetAll");
    }
}
```

---

## 🔧 Advanced: Multiple Files Per Repository

```csharp
// Program.cs - Khai báo nhiều files cho 1 key
options.ConfigureSqlMaps(provider =>
{
    provider.AddFiles(
        ("OrderFull", SqlMapFiles.DAO005, "Order queries"),
        ("OrderFull", SqlMapFiles.DAO006, "Order item queries")
    );
});

// Repository sẽ cần custom logic để load multiple files
// (Hiện tại ProviderBasedRepository chỉ support 1 key = 1 file)
```

---

## 🎯 Best Practices

### ✅ DO: Centralized Configuration

```csharp
// ✅ GOOD - Tất cả ở một chỗ
options.ConfigureSqlMaps(provider =>
{
    provider.AddFile("Order", SqlMapFiles.DAO005);
    provider.AddFile("Customer", SqlMapFiles.DAO010);
    provider.AddFile("Product", SqlMapFiles.DAO015);
});
```

### ❌ DON'T: Scattered Configuration

```csharp
// ❌ BAD - Hardcode ở nhiều chỗ
public class OrderRepo : ScopedRepo<Order>
{
    private const string SQL_MAP_FILE = SqlMapFiles.DAO005;  // ← Ở đây
}

public class CustomerRepo : ScopedRepo<Customer>
{
    private const string SQL_MAP_FILE = SqlMapFiles.DAO010;  // ← Lại ở đây
}
```

---

### ✅ DO: Meaningful Keys

```csharp
provider.AddFile("Order", SqlMapFiles.DAO005);          // ✅ Clear
provider.AddFile("Customer", SqlMapFiles.DAO010);       // ✅ Clear
provider.AddFile("ProductCatalog", SqlMapFiles.DAO015); // ✅ Descriptive
```

### ❌ DON'T: Cryptic Keys

```csharp
provider.AddFile("D005", SqlMapFiles.DAO005);  // ❌ What is D005?
provider.AddFile("Ord", SqlMapFiles.DAO010);   // ❌ Unclear
```

---

### ✅ DO: Add Descriptions

```csharp
provider.AddFile("Order", SqlMapFiles.DAO005, "Order management and processing");
provider.AddFile("Customer", SqlMapFiles.DAO010, "Customer data and profiles");
```

Descriptions help documentation and debugging!

---

## 📚 Migration Guide

### From Constants Pattern → Provider Pattern

**Before**:
```csharp
public class OrderRepository : ScopedSqlMapRepository<Order>
{
    private const string SQL_MAP_FILE = SqlMapFiles.DAO005;

    public OrderRepository(IDbSessionFactory sf)
        : base(sf, SQL_MAP_FILE) { }
}
```

**After**:

1. **Add to Program.cs**:
```csharp
options.ConfigureSqlMaps(provider =>
{
    provider.AddFile("Order", SqlMapFiles.DAO005);
});
```

2. **Update Repository**:
```csharp
public class OrderRepository : ProviderBasedRepository<Order>
{
    private const string MAP_KEY = "Order";

    public OrderRepository(IDbSessionFactory sf, SqlMapProvider provider)
        : base(sf, provider, MAP_KEY) { }

    // Remove: private const string SQL_MAP_FILE = ...
}
```

---

## ✅ Tóm Tắt

### Provider Pattern = Giống MrFu.Smartcheck!

1. **Khai báo TẬP TRUNG** trong `Program.cs`:
```csharp
options.ConfigureSqlMaps(provider =>
{
    provider.AddFile("Order", SqlMapFiles.DAO005);
});
```

2. **Repository dùng KEY**:
```csharp
public class OrderRepository : ProviderBasedRepository<Order>
{
    private const string MAP_KEY = "Order";

    public OrderRepository(IDbSessionFactory sf, SqlMapProvider provider)
        : base(sf, provider, MAP_KEY) { }
}
```

3. **Done!** ✅

---

### Lợi Ích:
- ✅ Tất cả SQL maps khai báo TẬP TRUNG
- ✅ Repository chỉ cần KEY
- ✅ Dễ quản lý, dễ thay đổi
- ✅ **Enterprise-ready pattern!**

**Giống MrFu.Smartcheck - Clean & Professional!** 🎉

---

## 🚀 Complete Working Examples

### ASP.NET Core Web API Example

Xem file: `samples/WSC.DataAccess.RealDB.Test/ProviderPatternProgramExample.cs`

```csharp
var builder = WebApplication.CreateBuilder();

// Logging
builder.Logging.AddIBatisLogging("log/iBatis", LogLevel.Information);

// Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;

// ✨ Provider Pattern Configuration
builder.Services.AddWscDataAccess(connectionString, options =>
{
    options.ConfigureSqlMaps(provider =>
    {
        // Order Domain
        provider.AddFile("Order", SqlMapFiles.DAO005, "Order management");
        provider.AddFile("OrderItem", SqlMapFiles.DAO006, "Order items");

        // Customer Domain
        provider.AddFile("Customer", SqlMapFiles.DAO010, "Customer data");

        // Product Domain
        provider.AddFile("Product", SqlMapFiles.DAO015, "Product catalog");
        provider.AddFile("Inventory", SqlMapFiles.DAO016, "Inventory tracking");

        // Payment & Shipping
        provider.AddFile("Payment", SqlMapFiles.DAO017, "Payment processing");
        provider.AddFile("Shipping", SqlMapFiles.DAO018, "Shipping info");

        // Reports
        provider.AddFile("Report", SqlMapFiles.DAO020, "Business reports");
    });
});

// Repositories
builder.Services.AddScoped<ProviderOrderRepository>();
builder.Services.AddScoped<ProviderCustomerRepository>();

var app = builder.Build();
app.MapControllers();
app.Run();
```

---

### Console Application Example

```csharp
var services = new ServiceCollection();

// Logging
services.AddLogging(builder =>
{
    builder.AddIBatisLogging("log/iBatis", LogLevel.Information);
    builder.AddConsole();
});

// Connection
var connectionString = "Server=localhost;Database=MyDB;...";

// Provider Pattern
services.AddWscDataAccess(connectionString, options =>
{
    options.ConfigureSqlMaps(provider =>
    {
        provider.AddFile("Order", SqlMapFiles.DAO005);
        provider.AddFile("Customer", SqlMapFiles.DAO010);
    });
});

// Repositories
services.AddScoped<ProviderOrderRepository>();

// Build & Use
var serviceProvider = services.BuildServiceProvider();

using (var scope = serviceProvider.CreateScope())
{
    var orderRepo = scope.ServiceProvider.GetRequiredService<ProviderOrderRepository>();
    var orders = await orderRepo.GetAllOrdersAsync();

    foreach (var order in orders)
    {
        Console.WriteLine($"Order ID: {order.Id}");
    }
}
```

---

## 🧪 Running the Demo

Chạy demo program để xem Provider Pattern hoạt động:

```bash
cd samples/WSC.DataAccess.RealDB.Test
dotnet run --project ProviderPatternDemo.cs
```

**Output sẽ hiển thị**:
```
========================================
✨ PROVIDER PATTERN DEMO
Giống MrFu.Smartcheck Provider Pattern!
========================================

✅ Registered SQL Maps:
   - Order     -> DAO005.xml
   - Customer  -> DAO010.xml
   - Product   -> DAO015.xml
   - Inventory -> DAO016.xml
   - Report    -> DAO020.xml

========================================
📋 TESTING PROVIDER PATTERN
========================================

Test 1: Order Repository (uses 'Order' key)
--------------------------------------------
✅ Retrieved 10 orders from DAO005.xml
   File: SqlMaps/DAO005.xml
   Key:  'Order'

Test 2: Customer Repository (uses 'Customer' key)
--------------------------------------------
✅ Retrieved 5 customers from DAO010.xml
   File: SqlMaps/DAO010.xml
   Key:  'Customer'

========================================
✅ ALL TESTS PASSED!
========================================
```

---

## 🔧 Troubleshooting

### Problem 1: "SQL map key 'XXX' not found in provider"

**Error Message**:
```
InvalidOperationException: SQL map key 'Order' not found in provider.
Please register it in ConfigureSqlMaps().
Example: provider.AddFile("Order", "SqlMaps/YourFile.xml")
```

**Solution**: Bạn quên đăng ký key trong `ConfigureSqlMaps`

```csharp
// ❌ BAD - Quên đăng ký
services.AddWscDataAccess(connectionString, options =>
{
    options.ConfigureSqlMaps(provider =>
    {
        // Empty - không có gì!
    });
});

// Repository dùng "Order" nhưng không tìm thấy!
// private const string MAP_KEY = "Order";

// ✅ FIX - Thêm registration
services.AddWscDataAccess(connectionString, options =>
{
    options.ConfigureSqlMaps(provider =>
    {
        provider.AddFile("Order", SqlMapFiles.DAO005);  // ← Thêm dòng này!
    });
});
```

---

### Problem 2: File không tồn tại

**Error Message**:
```
FileNotFoundException: Could not find file 'SqlMaps/DAO005.xml'
```

**Solution**: File SQL map không tồn tại hoặc path sai

```csharp
// Kiểm tra file có tồn tại:
if (SqlMapFiles.Exists(SqlMapFiles.DAO005))
{
    Console.WriteLine("File exists!");
}
else
{
    Console.WriteLine($"File NOT found: {SqlMapFiles.GetFullPath(SqlMapFiles.DAO005)}");
}

// Kiểm tra tất cả DAO files:
var allFiles = SqlMapFiles.GetAllDaoFiles();
foreach (var file in allFiles)
{
    Console.WriteLine($"{file}: {(SqlMapFiles.Exists(file) ? "✅" : "❌")}");
}
```

---

### Problem 3: Multiple keys cùng file path

**Question**: Có thể dùng 1 file cho nhiều keys không?

**Answer**: Có! Hoàn toàn được!

```csharp
options.ConfigureSqlMaps(provider =>
{
    // Cùng 1 file, nhiều keys (aliases)
    provider.AddFile("Order", SqlMapFiles.DAO005, "Order queries");
    provider.AddFile("OrderManagement", SqlMapFiles.DAO005, "Same file, different key");
    provider.AddFile("DAO005", SqlMapFiles.DAO005, "File code as key");
});

// 3 repositories dùng cùng file DAO005.xml
public class OrderRepository : ProviderBasedRepository<Order>
{
    private const string MAP_KEY = "Order";  // ← Key 1
}

public class OrderMgmtRepository : ProviderBasedRepository<Order>
{
    private const string MAP_KEY = "OrderManagement";  // ← Key 2
}

public class DAO005Repository : ProviderBasedRepository<Order>
{
    private const string MAP_KEY = "DAO005";  // ← Key 3
}
```

---

### Problem 4: SqlMapProvider not registered in DI

**Error Message**:
```
InvalidOperationException: Unable to resolve service for type 'SqlMapProvider'
```

**Solution**: Bạn quên gọi `AddWscDataAccess`

```csharp
// ❌ BAD
var services = new ServiceCollection();
services.AddScoped<ProviderOrderRepository>();  // ← SqlMapProvider chưa được đăng ký!

// ✅ FIX
var services = new ServiceCollection();
services.AddWscDataAccess(connectionString);  // ← Đăng ký SqlMapProvider
services.AddScoped<ProviderOrderRepository>();
```

---

## 📊 Performance Considerations

### Singleton vs Scoped

**SqlMapProvider** được đăng ký là **Singleton**:

```csharp
// In DataAccessServiceCollectionExtensions.cs
services.AddSingleton(options.SqlMapProvider);
```

**Why Singleton?**
- ✅ Configuration không thay đổi trong runtime
- ✅ Tiết kiệm memory (chỉ 1 instance)
- ✅ Thread-safe (read-only sau khi config)
- ✅ Fast lookup

**Repositories** thường là **Scoped**:

```csharp
services.AddScoped<ProviderOrderRepository>();
```

**Why Scoped?**
- ✅ Mỗi HTTP request có instance riêng
- ✅ Tránh concurrency issues
- ✅ DbSession được quản lý đúng

---

## 🎨 Advanced Patterns

### Pattern 1: Domain-Based Organization

```csharp
options.ConfigureSqlMaps(provider =>
{
    // ═══ Order Domain ═══
    provider.AddFile("Order.Main", SqlMapFiles.DAO005);
    provider.AddFile("Order.Items", SqlMapFiles.DAO006);
    provider.AddFile("Order.History", SqlMapFiles.DAO007);

    // ═══ Customer Domain ═══
    provider.AddFile("Customer.Main", SqlMapFiles.DAO010);
    provider.AddFile("Customer.Address", SqlMapFiles.DAO011);

    // ═══ Product Domain ═══
    provider.AddFile("Product.Catalog", SqlMapFiles.DAO015);
    provider.AddFile("Product.Inventory", SqlMapFiles.DAO016);
});
```

---

### Pattern 2: Environment-Based Configuration

```csharp
builder.Services.AddWscDataAccess(connectionString, options =>
{
    options.ConfigureSqlMaps(provider =>
    {
        // Common files (all environments)
        provider.AddFile("Order", SqlMapFiles.DAO005);
        provider.AddFile("Customer", SqlMapFiles.DAO010);

        // Environment-specific files
        if (builder.Environment.IsDevelopment())
        {
            provider.AddFile("DevTools", SqlMapFiles.DAO999);
        }

        if (builder.Environment.IsProduction())
        {
            provider.AddFile("Analytics", SqlMapFiles.DAO998);
        }
    });
});
```

---

### Pattern 3: Feature Flags

```csharp
options.ConfigureSqlMaps(provider =>
{
    // Core features (always enabled)
    provider.AddFile("Order", SqlMapFiles.DAO005);
    provider.AddFile("Customer", SqlMapFiles.DAO010);

    // Optional features (based on config)
    var featureFlags = builder.Configuration.GetSection("Features");

    if (featureFlags.GetValue<bool>("EnableReporting"))
    {
        provider.AddFile("Report", SqlMapFiles.DAO020);
    }

    if (featureFlags.GetValue<bool>("EnableInventory"))
    {
        provider.AddFile("Inventory", SqlMapFiles.DAO016);
    }
});
```

---

## 💡 Tips & Tricks

### Tip 1: Logging Registered Files

```csharp
services.AddWscDataAccess(connectionString, options =>
{
    options.ConfigureSqlMaps(provider =>
    {
        provider.AddFile("Order", SqlMapFiles.DAO005);
        provider.AddFile("Customer", SqlMapFiles.DAO010);

        // Log all registered files
        var logger = builder.Services.BuildServiceProvider()
            .GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Registered SQL Maps:");
        foreach (var file in provider.Files)
        {
            logger.LogInformation("  - {Key} -> {FilePath} ({Description})",
                file.Key, file.FilePath, file.Description ?? "No description");
        }
    });
});
```

---

### Tip 2: Validation on Startup

```csharp
// Validate all files exist on startup
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var provider = scope.ServiceProvider.GetRequiredService<SqlMapProvider>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Validating SQL map files...");

    foreach (var registration in provider.Files)
    {
        if (!SqlMapFiles.Exists(registration.FilePath))
        {
            logger.LogError("❌ File not found: {FilePath} (Key: {Key})",
                registration.FilePath, registration.Key);
            throw new FileNotFoundException($"SQL map file not found: {registration.FilePath}");
        }
        else
        {
            logger.LogInformation("✅ {Key} -> {FilePath}",
                registration.Key, registration.FilePath);
        }
    }

    logger.LogInformation("All SQL map files validated successfully!");
}

app.Run();
```

---

### Tip 3: Unit Testing

```csharp
[Fact]
public void Test_SqlMapProvider_Registration()
{
    // Arrange
    var provider = new SqlMapProvider();

    // Act
    provider.AddFile("Order", SqlMapFiles.DAO005);
    provider.AddFile("Customer", SqlMapFiles.DAO010);

    // Assert
    Assert.True(provider.HasFile("Order"));
    Assert.True(provider.HasFile("Customer"));
    Assert.False(provider.HasFile("NonExistent"));

    Assert.Equal(SqlMapFiles.DAO005, provider.GetFilePath("Order"));
    Assert.Equal(SqlMapFiles.DAO010, provider.GetFilePath("Customer"));
    Assert.Null(provider.GetFilePath("NonExistent"));
}

[Fact]
public void Test_Repository_Uses_Provider()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddWscDataAccess("Server=...;", options =>
    {
        options.ConfigureSqlMaps(provider =>
        {
            provider.AddFile("Order", SqlMapFiles.DAO005);
        });
    });
    services.AddScoped<ProviderOrderRepository>();

    var serviceProvider = services.BuildServiceProvider();

    // Act
    using var scope = serviceProvider.CreateScope();
    var repository = scope.ServiceProvider.GetRequiredService<ProviderOrderRepository>();

    // Assert
    Assert.NotNull(repository);
}
```

---

## 🔗 See Also

- **SIMPLE_REPOSITORY_GUIDE.md** - Attribute pattern (simplest, 4 lines)
- **SCOPED_SQLMAP_GUIDE.md** - Scoped SQL map pattern
- **SQLMAP_CONSTANTS_GUIDE.md** - Constants reference
- **IBATIS_LOGGING.md** - Logging configuration
- **ProviderPatternDemo.cs** - Working demo program
- **ProviderPatternProgramExample.cs** - Real-world Program.cs examples

---

## ✅ Checklist

Khi sử dụng Provider Pattern, đảm bảo:

- [ ] ✅ Đã gọi `AddWscDataAccess()` trong Program.cs
- [ ] ✅ Đã gọi `ConfigureSqlMaps()` để đăng ký SQL maps
- [ ] ✅ Repository extends `ProviderBasedRepository<T>`
- [ ] ✅ Repository inject `SqlMapProvider` trong constructor
- [ ] ✅ Repository pass correct `MAP_KEY` to base constructor
- [ ] ✅ MAP_KEY đã được đăng ký trong `ConfigureSqlMaps()`
- [ ] ✅ SQL map file tồn tại ở đường dẫn chỉ định
- [ ] ✅ Repository được đăng ký trong DI container

---

**✨ DONE! Provider Pattern hoàn chỉnh!**

**Giống MrFu.Smartcheck - Clean & Professional!** 🎉
```
