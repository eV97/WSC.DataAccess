# Multiple Connections Guide - Giống MrFu.SmartCheck!

Hướng dẫn sử dụng **multiple named connections** và **DAO constants** trong WSC.DataAccess.

---

## 🎯 Overview

WSC.DataAccess hỗ trợ **multiple named connections** để:
- ✅ Sử dụng nhiều databases trong 1 application
- ✅ Phân tách Main DB, Archive DB, Analytics DB
- ✅ Read/Write separation
- ✅ Multi-tenant applications

Giống với **MrFu.SmartCheck** pattern!

---

## 📋 Features

### 1. DAO Name Constants

Thay vì hardcode strings:

```csharp
// ❌ BAD - Hardcoded
public class GroupRepository
{
    private const string DAO_NAME = "DAO003";  // ← Dễ typo!
}
```

Sử dụng constants:

```csharp
// ✅ GOOD - Using DaoNames constants
using WSC.DataAccess.Constants;

public class GroupRepository
{
    private const string DAO_NAME = DaoNames.DAO003;  // ← IntelliSense!
}
```

### 2. Multiple Connections

Một app có thể dùng nhiều databases:

```csharp
// Program.cs
services.AddWscDataAccess(mainConnectionString, options =>
{
    // Additional connections
    options.AddConnection("Connection_2", archiveConnectionString);
    options.AddConnection("Connection_3", analyticsConnectionString);

    options.ConfigureSqlMaps(provider =>
    {
        // Main DB (Connection_1 = default)
        provider.AddFile("Order", SqlMapFiles.DAO005, "Connection_1");

        // Archive DB (Connection_2)
        provider.AddFile("Order", SqlMapFiles.DAO006, "Connection_2");

        // Analytics DB (Connection_3)
        provider.AddFile("Report", SqlMapFiles.DAO020, "Connection_3");
    });
});
```

---

## 🚀 Quick Start

### Step 1: Sử Dụng DAO Constants

**Import namespace**:

```csharp
using WSC.DataAccess.Constants;
```

**Sử dụng trong repository**:

```csharp
public class GroupRepository : ProviderBasedRepository<Group>
{
    // ✨ Sử dụng DaoNames constants
    private const string DAO_NAME = DaoNames.DAO003;

    public GroupRepository(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider)
        : base(sessionFactory, provider, DAO_NAME)
    {
    }

    public async Task<IEnumerable<Group>> GetGroupsByUserAsync(int userId)
    {
        var parameters = new { UserId = userId };

        // ✨ Clean code - no hardcoded strings!
        return await QueryListAsync("GetGroupsByUser", parameters);
    }
}
```

---

### Step 2: Cấu Hình Multiple Connections

**Program.cs / Startup.cs**:

```csharp
var builder = WebApplication.CreateBuilder();

// Main database connection
var mainConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;

// Archive database connection
var archiveConnectionString = builder.Configuration.GetConnectionString("ArchiveConnection")!;

// Analytics database connection
var analyticsConnectionString = builder.Configuration.GetConnectionString("AnalyticsConnection")!;

// ✨ Configure with multiple connections
builder.Services.AddWscDataAccess(mainConnectionString, options =>
{
    // Register additional connections
    options.AddConnection("Connection_2", archiveConnectionString);
    options.AddConnection("Connection_3", analyticsConnectionString);

    // Configure SQL maps for each connection
    options.ConfigureSqlMaps(provider =>
    {
        // ═══ Connection_1 (Main DB - Default) ═══
        provider.AddFile(DaoNames.ORDER, SqlMapFiles.DAO005, "Connection_1");
        provider.AddFile(DaoNames.CUSTOMER, SqlMapFiles.DAO010, "Connection_1");

        // ═══ Connection_2 (Archive DB) ═══
        provider.AddFile(DaoNames.ORDER, SqlMapFiles.DAO006, "Connection_2");
        provider.AddFile(DaoNames.CUSTOMER, SqlMapFiles.DAO011, "Connection_2");

        // ═══ Connection_3 (Analytics DB) ═══
        provider.AddFile(DaoNames.REPORT, SqlMapFiles.DAO020, "Connection_3");
    });
});
```

---

### Step 3: Repository Sử Dụng Multiple Connections

```csharp
public class MultiConnectionRepository : ProviderBasedRepository<dynamic>
{
    private const string CONNECTION_MAIN = "Connection_1";
    private const string CONNECTION_ARCHIVE = "Connection_2";
    private const string CONNECTION_ANALYTICS = "Connection_3";

    public MultiConnectionRepository(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider)
        : base(sessionFactory, provider, DaoNames.ORDER, CONNECTION_MAIN)
    {
    }

    // ═══ Main Database ═══
    public async Task<IEnumerable<Order>> GetActiveOrdersAsync()
    {
        // ✨ Sử dụng Connection_1 (Main DB)
        return await QueryListAsync<Order>("Order.GetAll", null, CONNECTION_MAIN);
    }

    // ═══ Archive Database ═══
    public async Task<IEnumerable<Order>> GetArchivedOrdersAsync()
    {
        // ✨ Sử dụng Connection_2 (Archive DB)
        return await QueryListAsync<Order>("Order.GetAll", null, CONNECTION_ARCHIVE);
    }

    // ═══ Analytics Database ═══
    public async Task<IEnumerable<Report>> GetSalesReportAsync()
    {
        // ✨ Sử dụng Connection_3 (Analytics DB)
        return await QueryListAsync<Report>("Report.Sales", null, CONNECTION_ANALYTICS);
    }

    // ═══ Cross-Database Operations ═══
    public async Task<AllOrdersReport> GetAllOrdersAsync()
    {
        // Lấy từ Main DB
        var activeOrders = await QueryListAsync<Order>("Order.GetAll", null, CONNECTION_MAIN);

        // Lấy từ Archive DB
        var archivedOrders = await QueryListAsync<Order>("Order.GetAll", null, CONNECTION_ARCHIVE);

        return new AllOrdersReport
        {
            ActiveOrders = activeOrders,
            ArchivedOrders = archivedOrders
        };
    }
}
```

---

## 📚 DaoNames Constants Reference

### Available Constants

```csharp
// DAO Numbers (DAO000 - DAO020)
DaoNames.DAO000
DaoNames.DAO001
DaoNames.DAO002
DaoNames.DAO003
DaoNames.DAO004
DaoNames.DAO005
DaoNames.DAO006
...
DaoNames.DAO020

// Named DAOs
DaoNames.ORDER       // "Order"
DaoNames.CUSTOMER    // "Customer"
DaoNames.PRODUCT     // "Product"
DaoNames.INVENTORY   // "Inventory"
DaoNames.PAYMENT     // "Payment"
DaoNames.SHIPPING    // "Shipping"
DaoNames.USER        // "User"
DaoNames.GROUP       // "Group"
DaoNames.APPLICATION // "Application"
DaoNames.REPORT      // "Report"
DaoNames.GENERIC     // "Generic"
```

### Helper Methods

```csharp
// Lấy tất cả DAO numbers
var allDaoNumbers = DaoNames.GetAllDaoNumbers();
// ["DAO000", "DAO001", ... "DAO020"]

// Lấy tất cả named DAOs
var allNamedDaos = DaoNames.GetAllNamedDaos();
// ["Order", "Customer", "Product", ...]

// Kiểm tra DAO name hợp lệ
bool isValid = DaoNames.IsValid("DAO003");  // true
bool isValid2 = DaoNames.IsValid("ORDER");   // true
bool isValid3 = DaoNames.IsValid("XYZ");     // false
```

---

## 🆚 Comparison: Hardcoded vs Constants

### ❌ Hardcoded (Bad)

```csharp
public class GroupRepository
{
    private const string DAO_NAME = "DAO003";  // ← No IntelliSense, easy typo

    public async Task<IEnumerable<Group>> GetGroupsByUserAsync(int userId)
    {
        // ❌ Hardcoded statement ID
        return await _daoProvider.ExecuteQueryAsync<Group>("DAO003", "GetGroupsByUser", new { UserId = userId });
    }
}
```

**Problems**:
- ❌ No IntelliSense
- ❌ Easy to make typos
- ❌ Hard to refactor
- ❌ Magic strings everywhere

---

### ✅ With Constants (Good)

```csharp
using WSC.DataAccess.Constants;

public class GroupRepository : ProviderBasedRepository<Group>
{
    private const string DAO_NAME = DaoNames.DAO003;  // ← IntelliSense!

    public GroupRepository(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider)
        : base(sessionFactory, provider, DAO_NAME)
    {
    }

    public async Task<IEnumerable<Group>> GetGroupsByUserAsync(int userId)
    {
        var parameters = new { UserId = userId };

        // ✅ Clean, no hardcoded strings
        return await QueryListAsync("GetGroupsByUser", parameters);
    }
}
```

**Benefits**:
- ✅ IntelliSense support
- ✅ Type-safe
- ✅ Easy to refactor
- ✅ No magic strings

---

## 🎯 Use Cases

### Use Case 1: Read/Write Separation

```csharp
services.AddWscDataAccess(mainConnectionString, options =>
{
    options.AddConnection("WriteDB", writeConnectionString);
    options.AddConnection("ReadDB", readConnectionString);

    options.ConfigureSqlMaps(provider =>
    {
        // Write operations -> WriteDB
        provider.AddFile("OrderWrite", SqlMapFiles.DAO005, "WriteDB");

        // Read operations -> ReadDB (replica)
        provider.AddFile("OrderRead", SqlMapFiles.DAO006, "ReadDB");
    });
});
```

**Repository**:

```csharp
public class OrderRepository : ProviderBasedRepository<Order>
{
    public async Task<IEnumerable<Order>> GetAllAsync()
    {
        // ✨ Read from ReadDB
        return await QueryListAsync("Order.GetAll", null, "ReadDB");
    }

    public async Task<int> CreateAsync(Order order)
    {
        // ✨ Write to WriteDB
        return await ExecuteAsync("Order.Insert", order, "WriteDB");
    }
}
```

---

### Use Case 2: Archive Old Data

```csharp
services.AddWscDataAccess(mainConnectionString, options =>
{
    options.AddConnection("MainDB", mainConnectionString);
    options.AddConnection("ArchiveDB", archiveConnectionString);

    options.ConfigureSqlMaps(provider =>
    {
        // Current data
        provider.AddFile(DaoNames.ORDER, SqlMapFiles.DAO005, "MainDB");

        // Historical data (older than 1 year)
        provider.AddFile(DaoNames.ORDER, SqlMapFiles.DAO006, "ArchiveDB");
    });
});
```

**Repository**:

```csharp
public async Task<IEnumerable<Order>> GetOrdersByYearAsync(int year)
{
    var currentYear = DateTime.Now.Year;

    if (year == currentYear)
    {
        // ✨ Current year -> MainDB
        return await QueryListAsync("Order.GetByYear", new { Year = year }, "MainDB");
    }
    else
    {
        // ✨ Old data -> ArchiveDB
        return await QueryListAsync("Order.GetByYear", new { Year = year }, "ArchiveDB");
    }
}
```

---

### Use Case 3: Dedicated Analytics Database

```csharp
services.AddWscDataAccess(mainConnectionString, options =>
{
    options.AddConnection("OLTP", oltpConnectionString);      // Transactional
    options.AddConnection("OLAP", olapConnectionString);      // Analytical

    options.ConfigureSqlMaps(provider =>
    {
        // OLTP - Transactional queries
        provider.AddFile(DaoNames.ORDER, SqlMapFiles.DAO005, "OLTP");
        provider.AddFile(DaoNames.CUSTOMER, SqlMapFiles.DAO010, "OLTP");

        // OLAP - Analytical queries
        provider.AddFile(DaoNames.REPORT, SqlMapFiles.DAO020, "OLAP");
    });
});
```

**Repository**:

```csharp
public async Task<SalesReport> GetSalesReportAsync(DateTime fromDate, DateTime toDate)
{
    // ✨ Heavy analytics query -> OLAP database
    var data = await QueryListAsync(
        "Report.SalesByPeriod",
        new { FromDate = fromDate, ToDate = toDate },
        "OLAP"
    );

    return new SalesReport { Data = data };
}
```

---

### Use Case 4: Multi-Tenant Application

```csharp
public class TenantRepository : ProviderBasedRepository<dynamic>
{
    private readonly IHttpContextAccessor _httpContext;

    public TenantRepository(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider,
        IHttpContextAccessor httpContext)
        : base(sessionFactory, provider, DaoNames.CUSTOMER)
    {
        _httpContext = httpContext;
    }

    public async Task<IEnumerable<Customer>> GetCustomersAsync()
    {
        // ✨ Dynamic connection based on tenant
        var tenantId = _httpContext.HttpContext?.User.FindFirst("TenantId")?.Value;
        var connectionName = $"Tenant_{tenantId}";

        return await QueryListAsync<Customer>("Customer.GetAll", null, connectionName);
    }
}

// Program.cs - Register connections per tenant
options.AddConnection("Tenant_1", tenant1ConnectionString);
options.AddConnection("Tenant_2", tenant2ConnectionString);
options.AddConnection("Tenant_3", tenant3ConnectionString);
```

---

## 🔧 Advanced Patterns

### Pattern 1: Connection Factory

```csharp
public class ConnectionSelector
{
    private readonly IConfiguration _config;

    public ConnectionSelector(IConfiguration config)
    {
        _config = config;
    }

    public string GetConnectionForOperation(string operation)
    {
        return operation switch
        {
            "Read" => "ReadDB",
            "Write" => "WriteDB",
            "Report" => "AnalyticsDB",
            "Archive" => "ArchiveDB",
            _ => SqlMapProvider.DEFAULT_CONNECTION
        };
    }
}
```

---

### Pattern 2: Dynamic Connection Switching

```csharp
public class SmartRepository : ProviderBasedRepository<Order>
{
    private readonly ConnectionSelector _connectionSelector;

    public SmartRepository(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider,
        ConnectionSelector connectionSelector)
        : base(sessionFactory, provider, DaoNames.ORDER)
    {
        _connectionSelector = connectionSelector;
    }

    public async Task<IEnumerable<Order>> GetOrdersAsync()
    {
        // ✨ Tự động chọn connection
        var connection = _connectionSelector.GetConnectionForOperation("Read");
        return await QueryListAsync("Order.GetAll", null, connection);
    }

    public async Task<int> CreateOrderAsync(Order order)
    {
        // ✨ Write operation -> WriteDB
        var connection = _connectionSelector.GetConnectionForOperation("Write");
        return await ExecuteAsync("Order.Insert", order, connection);
    }
}
```

---

## 📊 Connection Management

### Checking Connections

```csharp
var provider = serviceProvider.GetRequiredService<SqlMapProvider>();

// Get all connection names
var connections = provider.GetAllConnectionNames();
Console.WriteLine($"Total connections: {connections.Length}");
// Output: ["Connection_1", "Connection_2", "Connection_3"]

// Get files by connection
var mainDbFiles = provider.GetFilesByConnection("Connection_1");
foreach (var file in mainDbFiles)
{
    Console.WriteLine($"{file.Key} -> {file.FilePath}");
}
```

---

### Validation

```csharp
// Validate connection exists
if (provider.HasFile(DaoNames.ORDER, "Connection_1"))
{
    Console.WriteLine("Order DAO registered for Connection_1");
}

// Get specific file
var registration = provider.GetRegistration(DaoNames.ORDER, "Connection_1");
if (registration != null)
{
    Console.WriteLine($"File: {registration.FilePath}");
    Console.WriteLine($"Description: {registration.Description}");
    Console.WriteLine($"Registered: {registration.RegisteredAt}");
}
```

---

## 💡 Best Practices

### ✅ DO: Use Constants

```csharp
// ✅ GOOD
private const string DAO_NAME = DaoNames.DAO003;
```

```csharp
// ❌ BAD
private const string DAO_NAME = "DAO003";
```

---

### ✅ DO: Name Connections Meaningfully

```csharp
// ✅ GOOD - Clear purpose
options.AddConnection("MainDB", ...);
options.AddConnection("ArchiveDB", ...);
options.AddConnection("AnalyticsDB", ...);
```

```csharp
// ❌ BAD - Unclear
options.AddConnection("DB1", ...);
options.AddConnection("DB2", ...);
options.AddConnection("DB3", ...);
```

---

### ✅ DO: Document Connection Purpose

```csharp
// ✅ GOOD
options.ConfigureSqlMaps(provider =>
{
    // Main database - current active data
    provider.AddFile(DaoNames.ORDER, SqlMapFiles.DAO005, "MainDB", "Active orders");

    // Archive database - historical data (older than 1 year)
    provider.AddFile(DaoNames.ORDER, SqlMapFiles.DAO006, "ArchiveDB", "Archived orders");
});
```

---

### ✅ DO: Validate Connections on Startup

```csharp
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var provider = scope.ServiceProvider.GetRequiredService<SqlMapProvider>();

    // Validate all connections
    var connections = provider.GetAllConnectionNames();
    foreach (var conn in connections)
    {
        var files = provider.GetFilesByConnection(conn);
        Console.WriteLine($"✅ {conn}: {files.Count()} files");
    }
}

app.Run();
```

---

## 🧪 Running the Demo

```bash
cd samples/WSC.DataAccess.RealDB.Test
dotnet run --project MultiConnectionDemo.cs
```

**Output**:
```
========================================
✨ MULTIPLE CONNECTIONS DEMO
========================================

🔌 Configuring Multiple Connections:
✅ Connection_1 (Main): WSC_Main
✅ Connection_2 (Archive): WSC_Archive
✅ Connection_3 (Analytics): WSC_Analytics

📋 Registering SQL Maps:
  Connection_1 (Main):
    - Order -> SqlMaps/DAO005.xml
    - Customer -> SqlMaps/DAO010.xml

  Connection_2 (Archive):
    - Order -> SqlMaps/DAO006.xml

  Connection_3 (Analytics):
    - Report -> SqlMaps/DAO020.xml

✅ ALL TESTS PASSED!
```

---

## 📝 appsettings.json Example

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=WSC_Main;...",
    "ArchiveConnection": "Server=localhost;Database=WSC_Archive;...",
    "AnalyticsConnection": "Server=localhost;Database=WSC_Analytics;..."
  }
}
```

---

## 🔗 See Also

- **PROVIDER_PATTERN_GUIDE.md** - Provider pattern basics
- **REPOSITORY_PATTERNS_SUMMARY.md** - All patterns comparison
- **SIMPLE_REPOSITORY_GUIDE.md** - Attribute pattern
- **GroupRepository.cs** - Example using DaoNames
- **MultiConnectionRepository.cs** - Example with multiple connections
- **MultiConnectionDemo.cs** - Working demo

---

## ✅ Checklist

When using multiple connections:

- [ ] ✅ Import `using WSC.DataAccess.Constants;`
- [ ] ✅ Use `DaoNames.XXX` instead of hardcoded strings
- [ ] ✅ Register all connections in `AddWscDataAccess()`
- [ ] ✅ Map SQL files to correct connections
- [ ] ✅ Pass connection name when calling `QueryListAsync()`, `ExecuteAsync()`
- [ ] ✅ Document purpose of each connection
- [ ] ✅ Validate connections on startup

---

**✨ DONE! Multiple Connections + DAO Constants!**

**Giống MrFu.SmartCheck - Professional & Scalable!** 🎉
