# WSC.DataAccess

Thư viện Data Access Layer cho .NET Core - Hỗ trợ SQL Server với IBatis-style XML mapping và dependency injection.

## Tổng quan

WSC.DataAccess là thư viện truy xuất dữ liệu mạnh mẽ, dễ sử dụng, hỗ trợ:

- **IBatis-style XML Mapping** - Định nghĩa SQL queries trong file XML
- **Multiple Database Connections** - Kết nối đến nhiều database cùng lúc
- **Dependency Injection** - Tích hợp hoàn toàn với Microsoft.Extensions.DependencyInjection
- **Transaction Support** - Hỗ trợ transactions đơn giản và an toàn
- **Logging** - Tích hợp Microsoft.Extensions.Logging cho debugging
- **Auto-Discovery** - Tự động scan và đăng ký SQL Map files

## Cài đặt

```bash
dotnet add package WSC.DataAccess
```

## Quick Start

### 1. Tạo SQL Map File

Tạo file `SqlMaps/DAO001.xml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<sqlMap namespace="User">
  <!-- Get all users -->
  <select id="User.GetAllUsers" resultClass="User">
    <![CDATA[
      SELECT Id, Username, FullName, Email, IsActive
      FROM Users
      ORDER BY FullName
    ]]>
  </select>

  <!-- Get user by ID -->
  <select id="User.GetUserById" resultClass="User">
    <![CDATA[
      SELECT Id, Username, FullName, Email, IsActive
      FROM Users
      WHERE Id = @Id
    ]]>
  </select>
</sqlMap>
```

### 2. Cấu hình appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=MyDB;User Id=sa;Password=***;TrustServerCertificate=True"
  }
}
```

### 3. Đăng ký Service trong Program.cs

```csharp
using WSC.DataAccess.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký WSC.DataAccess với auto-discovery
builder.Services.AddWscDataAccess(
    builder.Configuration,
    configure: options =>
    {
        // Tự động scan và register tất cả SQL Map files
        options.AutoDiscoverSqlMaps("SqlMaps");
    });
```

### 4. Sử dụng trong Service

```csharp
using WSC.DataAccess.Core;
using WSC.DataAccess.Extensions;

public class UserService
{
    private readonly ISql _sql;

    public UserService(ISql sql)
    {
        _sql = sql;
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        // Set DAO context
        _sql.GetDAO("DAO001");

        // Create connection và execute query
        using var connection = _sql.CreateConnection();
        return await connection.StatementExecuteQueryAsync<User>("User.GetAllUsers");
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        _sql.GetDAO("DAO001");
        using var connection = _sql.CreateConnection();
        return await connection.StatementExecuteSingleAsync<User>(
            "User.GetUserById",
            new { Id = userId });
    }
}
```

## Các tính năng chính

### ISql Pattern - API đơn giản và mạnh mẽ

```csharp
// Inject ISql
public class MyService
{
    private readonly ISql _sql;

    public MyService(ISql sql) => _sql = sql;

    public async Task DoWorkAsync()
    {
        // 1. Set DAO context
        _sql.GetDAO("DAO001");

        // 2. Create connection
        using var conn = _sql.CreateConnection();

        // 3. Execute queries
        var users = await conn.StatementExecuteQueryAsync<User>("User.GetAll");
        var user = await conn.StatementExecuteSingleAsync<User>("User.GetById", new { Id = 1 });
        var count = await conn.StatementExecuteScalarAsync<int>("User.Count");
        await conn.StatementExecuteAsync("User.Insert", newUser);
    }
}
```

### Multiple Database Connections

```csharp
// Kết nối đến nhiều database
builder.Services.AddWscDataAccess(builder.Configuration, options =>
{
    options.AutoDiscoverSqlMaps("SqlMaps");
    // Connection strings tự động load từ appsettings.json:
    // DefaultConnection -> "Default"
    // HISConnection -> "HIS"
    // LISConnection -> "LIS"
});

// Sử dụng
public async Task QueryMultipleDatabasesAsync()
{
    _sql.GetDAO("DAO001");

    // Query from Default database
    using var conn1 = _sql.CreateConnection();
    var users = await conn1.StatementExecuteQueryAsync<User>("User.GetAll");

    // Query from HIS database
    using var conn2 = _sql.CreateConnection("HIS");
    var patients = await conn2.StatementExecuteQueryAsync<Patient>("Patient.GetAll");

    // Query from LIS database
    using var conn3 = _sql.CreateConnection("LIS");
    var tests = await conn3.StatementExecuteQueryAsync<Test>("Test.GetAll");
}
```

### Transaction Support

```csharp
public async Task CreateOrderWithItemsAsync(Order order, List<OrderItem> items)
{
    _sql.GetDAO("DAO003");
    using var connection = _sql.CreateConnection();

    await connection.ExecuteInTransactionAsync(async conn =>
    {
        // Insert order
        await conn.StatementExecuteAsync("Order.Insert", order);

        // Insert order items
        foreach (var item in items)
        {
            await conn.StatementExecuteAsync("OrderItem.Insert", item);
        }

        // Transaction tự động commit nếu không có exception
        // Tự động rollback nếu có exception
    });
}
```

### Provider Pattern - DAO Names Management

```csharp
public static class Provider
{
    public const string DAO000 = "DAO000"; // System queries
    public const string DAO001 = "DAO001"; // User management
    public const string DAO002 = "DAO002"; // Product management

    public static string[] GetDaoFiles(string baseDirectory)
    {
        return new[] { DAO000, DAO001, DAO002 }
            .Select(dao => Path.Combine(baseDirectory, $"{dao}.xml"))
            .ToArray();
    }
}

// Sử dụng
_sql.GetDAO(Provider.DAO001);
```

## Configuration Options

### Option 1: IConfiguration (Khuyến nghị)

```csharp
builder.Services.AddWscDataAccess(builder.Configuration, options =>
{
    options.AutoDiscoverSqlMaps("SqlMaps");
});
```

### Option 2: IConfigurationSection

```csharp
var connectionStringsSection = builder.Configuration.GetSection("ConnectionStrings");
builder.Services.AddWscDataAccess(connectionStringsSection, options =>
{
    options.AutoDiscoverSqlMaps("SqlMaps");
});
```

### Option 3: Dictionary (Runtime configuration)

```csharp
var connectionStrings = new Dictionary<string, string>
{
    { "Default", "Server=...;Database=DB1" },
    { "HIS", "Server=...;Database=HIS" },
    { "LIS", "Server=...;Database=LIS" }
};

builder.Services.AddWscDataAccess(
    connectionStrings,
    defaultConnectionName: "Default",
    options => options.AutoDiscoverSqlMaps("SqlMaps"));
```

### Option 4: Single Connection String

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddWscDataAccess(connectionString, options =>
{
    options.AutoDiscoverSqlMaps("SqlMaps");
});
```

## SQL Map Registration Patterns

### Pattern 1: Auto-Discovery (Khuyến nghị - Đơn giản nhất)

```csharp
options.AutoDiscoverSqlMaps("SqlMaps");
```

### Pattern 2: Provider.GetDaoFiles() (Legacy pattern)

```csharp
var daoFiles = Provider.GetDaoFiles("SqlMaps");
var existingFiles = daoFiles.Where(File.Exists).ToArray();

options.ConfigureSqlMaps(provider =>
{
    foreach (var file in existingFiles)
    {
        var fileName = Path.GetFileNameWithoutExtension(file);
        provider.AddFile(fileName, file);
    }
});
```

### Pattern 3: Manual Registration (Full control)

```csharp
options.ConfigureSqlMaps(provider =>
{
    provider.AddFile("DAO000", "SqlMaps/DAO000.xml", "System queries");
    provider.AddFile("DAO001", "SqlMaps/DAO001.xml", "User management");
    provider.AddFile("DAO002", "SqlMaps/DAO002.xml", "Product management");
});
```

## Sample Application

Xem các ví dụ chi tiết trong thư mục `samples/WSC.DataAccess.Sample/`:

- **Program.cs** - ConfigurationSection pattern
- **Program_Dictionary.cs** - Dictionary pattern
- **Program_Configs.cs** - IConfiguration pattern
- **Program_Alternative.cs** - Provider.GetDaoFiles() pattern
- **TestService.cs** - Các ví dụ sử dụng ISql API

## Tài liệu chi tiết

- [Getting Started](GETTING_STARTED.md) - Hướng dẫn bắt đầu chi tiết
- [Configuration](CONFIGURATION.md) - Hướng dẫn cấu hình
- [Examples](EXAMPLES.md) - Các ví dụ sử dụng từ test apps
- [API Reference](API_REFERENCE.md) - Tài liệu API chi tiết
- [Architecture](ARCHITECTURE.md) - Kiến trúc dự án
- [FAQ](FAQ.md) - Câu hỏi thường gặp

## Requirements

- .NET 6.0 trở lên
- SQL Server 2016 trở lên

## License

MIT License - See LICENSE file for details
