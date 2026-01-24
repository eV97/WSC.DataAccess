# Configuration Guide - WSC.DataAccess

Hướng dẫn chi tiết về các cách cấu hình WSC.DataAccess.

## Mục lục

- [Connection String Configuration](#connection-string-configuration)
- [SQL Map Registration](#sql-map-registration)
- [Multiple Database Connections](#multiple-database-connections)
- [Logging Configuration](#logging-configuration)
- [Advanced Configuration](#advanced-configuration)

---

## Connection String Configuration

WSC.DataAccess hỗ trợ 4 cách cấu hình connection strings.

### Option 1: IConfiguration (Khuyến nghị)

Sử dụng toàn bộ IConfiguration object - Tự động load tất cả connection strings từ appsettings.json.

**appsettings.json:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=DB1;...",
    "HISConnection": "Server=.;Database=HIS;...",
    "LISConnection": "Server=.;Database=LIS;..."
  }
}
```

**Program.cs:**

```csharp
using WSC.DataAccess.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWscDataAccess(
    builder.Configuration,  // Pass entire IConfiguration
    configure: options =>
    {
        options.AutoDiscoverSqlMaps("SqlMaps");
    });
```

**Connection Name Mapping:**
- `DefaultConnection` → `"Default"`
- `HISConnection` → `"HIS"`
- `LISConnection` → `"LIS"`
- Suffix "Connection" tự động được remove

**Usage:**

```csharp
// Default connection
using var conn1 = _sql.CreateConnection();

// Named connections
using var conn2 = _sql.CreateConnection("HIS");
using var conn3 = _sql.CreateConnection("LIS");
```

**Sample:** See `samples/WSC.DataAccess.Sample/Program_Configs.cs`

---

### Option 2: IConfigurationSection

Chỉ truyền ConnectionStrings section thay vì toàn bộ configuration.

**Program.cs:**

```csharp
using WSC.DataAccess.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Get ConnectionStrings section
var connectionStringsSection = builder.Configuration.GetSection("ConnectionStrings");

builder.Services.AddWscDataAccess(
    connectionStringsSection,  // Pass section only
    configure: options =>
    {
        options.AutoDiscoverSqlMaps("SqlMaps");
    });
```

**Khi nào dùng:**
- Khi bạn muốn rõ ràng chỉ truyền connection strings
- Khi configuration có nhiều sections khác không liên quan

**Sample:** See `samples/WSC.DataAccess.Sample/Program.cs` và `Program_ConfigSection.cs`

---

### Option 3: Dictionary<string, string> (Runtime configuration)

Tạo connection strings động tại runtime - Hữu ích khi load từ database, API, hoặc environment variables.

**Program.cs:**

```csharp
using WSC.DataAccess.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Option 3a: Build from IConfiguration
var connectionStrings = new Dictionary<string, string>();
var section = builder.Configuration.GetSection("ConnectionStrings");

foreach (var conn in section.GetChildren())
{
    var value = conn.Value;
    if (!string.IsNullOrWhiteSpace(value))
    {
        // Remove "Connection" suffix
        var cleanName = conn.Key.EndsWith("Connection", StringComparison.OrdinalIgnoreCase)
            ? conn.Key.Substring(0, conn.Key.Length - "Connection".Length)
            : conn.Key;

        connectionStrings[cleanName] = value;
    }
}

builder.Services.AddWscDataAccess(
    connectionStrings,
    defaultConnectionName: "Default",  // Required
    configure: options =>
    {
        options.AutoDiscoverSqlMaps("SqlMaps");
    });
```

**Option 3b: Load from Database/API:**

```csharp
// Load connection strings from database or API
var connectionStrings = await LoadConnectionStringsFromDatabaseAsync();

builder.Services.AddWscDataAccess(
    connectionStrings,
    defaultConnectionName: "Default",
    configure: options =>
    {
        options.AutoDiscoverSqlMaps("SqlMaps");
    });
```

**Option 3c: Load from Environment Variables:**

```csharp
var connectionStrings = new Dictionary<string, string>
{
    { "Default", Environment.GetEnvironmentVariable("DB_DEFAULT") ?? "" },
    { "HIS", Environment.GetEnvironmentVariable("DB_HIS") ?? "" },
    { "LIS", Environment.GetEnvironmentVariable("DB_LIS") ?? "" }
};

builder.Services.AddWscDataAccess(
    connectionStrings,
    defaultConnectionName: "Default",
    configure: options =>
    {
        options.AutoDiscoverSqlMaps("SqlMaps");
    });
```

**Khi nào dùng:**
- Load connection strings từ database/API
- Environment variables
- Testing scenarios
- Dynamic configuration

**Sample:** See `samples/WSC.DataAccess.Sample/Program_Dictionary.cs`

---

### Option 4: Single Connection String

Sử dụng 1 connection string duy nhất - Đơn giản nhất cho single database.

**Program.cs:**

```csharp
using WSC.DataAccess.Configuration;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection not found");

builder.Services.AddWscDataAccess(
    connectionString,
    configure: options =>
    {
        options.AutoDiscoverSqlMaps("SqlMaps");
    });
```

**Usage:**

```csharp
// Chỉ có default connection
using var connection = _sql.CreateConnection();
```

**Khi nào dùng:**
- Ứng dụng chỉ kết nối 1 database duy nhất
- Prototype/Demo projects
- Simple applications

---

## SQL Map Registration

WSC.DataAccess hỗ trợ 3 patterns để đăng ký SQL Map files.

### Pattern 1: Auto-Discovery (Khuyến nghị)

Tự động scan thư mục và đăng ký tất cả .xml files.

```csharp
builder.Services.AddWscDataAccess(configuration, options =>
{
    // Auto-discover all .xml files in SqlMaps directory
    options.AutoDiscoverSqlMaps("SqlMaps");
});
```

**Cách hoạt động:**
1. Scan thư mục `SqlMaps/`
2. Tìm tất cả file `*.xml`
3. Tự động register với name = filename (không extension)

**Ví dụ:**
- `SqlMaps/DAO000.xml` → DAO name = `"DAO000"`
- `SqlMaps/DAO001.xml` → DAO name = `"DAO001"`
- `SqlMaps/UserDAO.xml` → DAO name = `"UserDAO"`

**Ưu điểm:**
- Đơn giản nhất
- Tự động phát hiện file mới
- Không cần maintain danh sách files

**Nhược điểm:**
- Không kiểm soát được thứ tự đăng ký
- Không thể bỏ qua files cụ thể

---

### Pattern 2: Provider.GetDaoFiles() (Legacy)

Sử dụng Provider class để quản lý DAO names - Giống pattern cũ trong MrFu.SmartCheck.Web.

**Provider.cs:**

```csharp
public static class Provider
{
    public const string DAO000 = "DAO000";
    public const string DAO001 = "DAO001";
    public const string DAO002 = "DAO002";

    public static string[] GetDaoFiles(string baseDirectory)
    {
        return new[] { DAO000, DAO001, DAO002 }
            .Select(dao => Path.Combine(baseDirectory, $"{dao}.xml"))
            .ToArray();
    }
}
```

**Program.cs:**

```csharp
var executingAssemblyLocation = Directory.GetCurrentDirectory();
var sqlMapsPath = Path.Combine(executingAssemblyLocation, "SqlMaps");

var daoFiles = Provider.GetDaoFiles(sqlMapsPath);
var existingFiles = daoFiles.Where(File.Exists).ToArray();

builder.Services.AddWscDataAccess(connectionString, options =>
{
    options.ConfigureSqlMaps(provider =>
    {
        foreach (var file in existingFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var description = Provider.GetDescription(fileName);
            provider.AddFile(fileName, file, description);
        }
    });
});
```

**Ưu điểm:**
- Kiểm soát chính xác files nào được load
- Kiểm tra file existence trước khi register
- Có thể thêm description cho mỗi DAO
- Pattern quen thuộc cho legacy projects

**Nhược điểm:**
- Phải maintain Provider class
- Thêm/xóa DAO cần update code

**Sample:** See `samples/WSC.DataAccess.Sample/Program_Alternative.cs`

---

### Pattern 3: Manual Registration (Full Control)

Đăng ký từng file thủ công - Full control.

```csharp
builder.Services.AddWscDataAccess(connectionString, options =>
{
    options.ConfigureSqlMaps(provider =>
    {
        provider.AddFile("DAO000", "SqlMaps/DAO000.xml", "System queries");
        provider.AddFile("DAO001", "SqlMaps/DAO001.xml", "User management");
        provider.AddFile("DAO002", "SqlMaps/DAO002.xml", "Product management");

        // Conditional registration
        if (someCondition)
        {
            provider.AddFile("DAO003", "SqlMaps/DAO003.xml", "Orders");
        }
    });
});
```

**Ưu điểm:**
- Full control
- Conditional registration
- Custom descriptions
- Custom file paths

**Nhược điểm:**
- Phải register từng file
- Dễ quên khi thêm DAO mới

**Khi nào dùng:**
- Cần conditional registration
- Custom file paths
- Testing specific DAOs
- Fine-grained control

---

## Multiple Database Connections

Cấu hình và sử dụng nhiều database connections.

### Cấu hình

**appsettings.json:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=ApplicationDB;User Id=sa;Password=***;TrustServerCertificate=True",
    "HISConnection": "Server=.;Database=HIS_Database;User Id=sa;Password=***;TrustServerCertificate=True",
    "LISConnection": "Server=.;Database=LIS_Database;User Id=sa;Password=***;TrustServerCertificate=True",
    "ReportConnection": "Server=ReportServer;Database=Reports;User Id=sa;Password=***;TrustServerCertificate=True"
  }
}
```

**Program.cs:**

```csharp
builder.Services.AddWscDataAccess(builder.Configuration, options =>
{
    options.AutoDiscoverSqlMaps("SqlMaps");
    // All connections auto-registered:
    // - Default, HIS, LIS, Report
});
```

### Sử dụng

```csharp
public class MultiDatabaseService
{
    private readonly ISql _sql;

    public MultiDatabaseService(ISql sql)
    {
        _sql = sql;
    }

    public async Task ProcessDataAsync()
    {
        // Set DAO context
        _sql.GetDAO("DAO001");

        // Query from Default database
        using (var conn1 = _sql.CreateConnection())
        {
            var users = await conn1.StatementExecuteQueryAsync<User>("User.GetAll");
        }

        // Query from HIS database
        using (var conn2 = _sql.CreateConnection("HIS"))
        {
            var patients = await conn2.StatementExecuteQueryAsync<Patient>("Patient.GetAll");
        }

        // Query from LIS database
        using (var conn3 = _sql.CreateConnection("LIS"))
        {
            var tests = await conn3.StatementExecuteQueryAsync<Test>("Test.GetAll");
        }

        // Query from Report database
        using (var conn4 = _sql.CreateConnection("Report"))
        {
            var reports = await conn4.StatementExecuteQueryAsync<Report>("Report.GetAll");
        }
    }
}
```

### Cross-Database Queries

```csharp
public async Task<OrderWithDetailsDTO> GetOrderWithDetailsAsync(int orderId)
{
    _sql.GetDAO("DAO003");

    // Get order from Default DB
    using var conn1 = _sql.CreateConnection();
    var order = await conn1.StatementExecuteSingleAsync<Order>(
        "Order.GetById",
        new { Id = orderId });

    // Get patient info from HIS DB
    using var conn2 = _sql.CreateConnection("HIS");
    var patient = await conn2.StatementExecuteSingleAsync<Patient>(
        "Patient.GetById",
        new { Id = order.PatientId });

    // Get test results from LIS DB
    using var conn3 = _sql.CreateConnection("LIS");
    var tests = await conn3.StatementExecuteQueryAsync<Test>(
        "Test.GetByOrderId",
        new { OrderId = orderId });

    // Combine results
    return new OrderWithDetailsDTO
    {
        Order = order,
        Patient = patient,
        Tests = tests.ToList()
    };
}
```

---

## Logging Configuration

### Basic Logging

**appsettings.json:**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "WSC.DataAccess": "Debug"
    }
  }
}
```

### Console Application Logging

```csharp
var services = new ServiceCollection();

services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);

    // WSC.DataAccess specific logging
    builder.AddFilter("WSC.DataAccess", LogLevel.Debug);
});

services.AddWscDataAccess(configuration, options =>
{
    options.AutoDiscoverSqlMaps("SqlMaps");
});
```

### Production Logging

```csharp
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();

    // Add Serilog or other providers
    loggingBuilder.AddSerilog();

    // Set log levels
    loggingBuilder.SetMinimumLevel(LogLevel.Information);
    loggingBuilder.AddFilter("WSC.DataAccess", LogLevel.Warning);
});
```

---

## Advanced Configuration

### Custom DAO Base Directory

```csharp
var customPath = Path.Combine(AppContext.BaseDirectory, "CustomMaps");

builder.Services.AddWscDataAccess(configuration, options =>
{
    options.AutoDiscoverSqlMaps(customPath);
});
```

### Conditional DAO Registration

```csharp
builder.Services.AddWscDataAccess(configuration, options =>
{
    options.ConfigureSqlMaps(provider =>
    {
        // Always register core DAOs
        provider.AddFile("DAO000", "SqlMaps/DAO000.xml", "System");
        provider.AddFile("DAO001", "SqlMaps/DAO001.xml", "Users");

        // Conditionally register feature DAOs
        if (builder.Configuration.GetValue<bool>("Features:EnableReports"))
        {
            provider.AddFile("DAO005", "SqlMaps/DAO005.xml", "Reports");
        }

        if (builder.Configuration.GetValue<bool>("Features:EnableAnalytics"))
        {
            provider.AddFile("DAO006", "SqlMaps/DAO006.xml", "Analytics");
        }
    });
});
```

### Environment-Specific Configuration

```csharp
var environment = builder.Environment.EnvironmentName;

builder.Services.AddWscDataAccess(configuration, options =>
{
    if (environment == "Development")
    {
        options.AutoDiscoverSqlMaps("SqlMaps");
    }
    else
    {
        // Production: explicit registration only
        options.ConfigureSqlMaps(provider =>
        {
            provider.AddFile("DAO000", "SqlMaps/DAO000.xml");
            provider.AddFile("DAO001", "SqlMaps/DAO001.xml");
        });
    }
});
```

### Docker/Container Configuration

**appsettings.json:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  }
}
```

**Program.cs:**

```csharp
var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not configured");

builder.Services.AddWscDataAccess(connectionString, options =>
{
    options.AutoDiscoverSqlMaps("SqlMaps");
});
```

**docker-compose.yml:**

```yaml
services:
  myapp:
    image: myapp:latest
    environment:
      - DATABASE_CONNECTION_STRING=Server=sqlserver;Database=MyDB;User Id=sa;Password=***
```

---

## Configuration Best Practices

### 1. Development vs Production

**Development:**
- Use auto-discovery
- Enable Debug logging
- Use local connection strings

**Production:**
- Use explicit registration or verified auto-discovery
- Set Warning/Error logging only
- Use environment variables or secrets management

### 2. Connection String Security

**DO:**
- Use User Secrets in development
- Use Azure Key Vault / AWS Secrets Manager in production
- Use environment variables in containers

**DON'T:**
- Commit connection strings to source control
- Hardcode passwords

### 3. Multiple Environments

```json
// appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=DevDB;..."
  }
}

// appsettings.Production.json
{
  "ConnectionStrings": {
    "DefaultConnection": ""  // Load from env vars
  }
}
```

---

## See Also

- [Getting Started](GETTING_STARTED.md) - Setup guide
- [Examples](EXAMPLES.md) - Usage examples
- [API Reference](API_REFERENCE.md) - API documentation
- [FAQ](FAQ.md) - Troubleshooting
