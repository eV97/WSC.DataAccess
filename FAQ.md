# FAQ - WSC.DataAccess

Câu hỏi thường gặp và hướng dẫn troubleshooting.

## Mục lục

- [Installation & Setup](#installation--setup)
- [Configuration Issues](#configuration-issues)
- [SQL Map Issues](#sql-map-issues)
- [Connection Issues](#connection-issues)
- [Query Execution Issues](#query-execution-issues)
- [Transaction Issues](#transaction-issues)
- [Best Practices](#best-practices)

---

## Installation & Setup

### Q: Làm sao để cài đặt WSC.DataAccess?

**A:** Sử dụng NuGet Package Manager:

```bash
dotnet add package WSC.DataAccess
```

Hoặc thêm vào `.csproj` file:

```xml
<PackageReference Include="WSC.DataAccess" Version="1.0.0" />
```

---

### Q: Cần .NET version nào?

**A:** .NET 6.0 trở lên.

---

### Q: Có support .NET Framework không?

**A:** Không. WSC.DataAccess chỉ support .NET Core/.NET 6+.

---

## Configuration Issues

### Q: "Connection string 'DefaultConnection' not found"

**A:** Kiểm tra `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=...;User Id=...;Password=..."
  }
}
```

Đảm bảo:
1. File `appsettings.json` có Copy to Output Directory = Copy if newer
2. Connection string name đúng (case-sensitive)
3. JSON syntax đúng (không missing comma, bracket)

---

### Q: "DAO 'DAO001' not found"

**A:** Có 3 nguyên nhân:

**1. SQL Map file không tồn tại:**

Kiểm tra file `SqlMaps/DAO001.xml` có tồn tại không.

**2. Chưa register SQL Map:**

```csharp
builder.Services.AddWscDataAccess(configuration, options =>
{
    options.AutoDiscoverSqlMaps("SqlMaps");  // Thiếu dòng này
});
```

**3. File XML không được copy to output:**

Kiểm tra `.csproj`:

```xml
<ItemGroup>
  <None Update="SqlMaps\*.xml">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

---

### Q: Auto-discovery không tìm thấy SQL Map files

**A:** Kiểm tra:

1. **Đường dẫn thư mục đúng:**

```csharp
// Sai
options.AutoDiscoverSqlMaps("SqlMaps/");  // ❌ Không có trailing slash
options.AutoDiscoverSqlMaps("/SqlMaps");  // ❌ Không bắt đầu với /

// Đúng
options.AutoDiscoverSqlMaps("SqlMaps");   // ✅
```

2. **Files có extension .xml:**

```
SqlMaps/
├── DAO000.xml  ✅
├── DAO001.xml  ✅
└── DAO002.txt  ❌ Sai extension
```

3. **Files được copy to output:**

Thêm vào `.csproj`:

```xml
<ItemGroup>
  <None Update="SqlMaps\**\*.xml">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

---

## SQL Map Issues

### Q: "Statement 'User.GetAll' not found in SQL map"

**A:** Kiểm tra:

**1. Statement ID đúng trong XML:**

```xml
<!-- Sai -->
<select id="GetAllUsers" resultClass="User">  <!-- ❌ Thiếu namespace -->

<!-- Đúng -->
<select id="User.GetAllUsers" resultClass="User">  <!-- ✅ Có namespace -->
```

**2. DAO được set đúng:**

```csharp
// Phải gọi GetDAO trước
_sql.GetDAO("DAO001");  // ✅
using var conn = _sql.CreateConnection();
var users = await conn.StatementExecuteQueryAsync<User>("User.GetAll");
```

**3. Namespace trong XML đúng:**

```xml
<sqlMap namespace="User">  <!-- namespace = "User" -->
  <select id="User.GetAll">  <!-- ID phải bắt đầu với "User." -->
```

---

### Q: XML parsing error

**A:** Kiểm tra XML syntax:

**Common mistakes:**

```xml
<!-- 1. Missing CDATA -->
<select id="User.GetAll">
  SELECT * FROM Users WHERE Name = 'John'  <!-- ❌ Không có CDATA, có thể lỗi với special chars -->
</select>

<!-- Đúng -->
<select id="User.GetAll">
  <![CDATA[
    SELECT * FROM Users WHERE Name = 'John'
  ]]>
</select>

<!-- 2. Invalid XML -->
<select id="User.GetAll">  <!-- ❌ Không đóng tag -->

<!-- 3. Wrong encoding -->
<?xml version="1.0" encoding="utf-16" ?>  <!-- ❌ Dùng utf-8 -->
```

---

### Q: Parameter không work (@Username, @Id, etc.)

**A:** Dapper sử dụng parameter names từ anonymous objects hoặc properties:

```csharp
// ✅ Đúng - Property names match parameters
await conn.StatementExecuteQueryAsync<User>(
    "User.GetById",
    new { Id = 123 });  // @Id trong SQL

// ❌ Sai - Property name không match
await conn.StatementExecuteQueryAsync<User>(
    "User.GetById",
    new { UserId = 123 });  // Tìm @UserId nhưng SQL có @Id

// SQL Map
<select id="User.GetById">
  <![CDATA[
    SELECT * FROM Users WHERE Id = @Id  -- Parameter name phải match
  ]]>
</select>
```

---

## Connection Issues

### Q: "Cannot open database"

**A:** Kiểm tra connection string:

**Common issues:**

```csharp
// 1. Wrong server name
"Server=localhost\\SQLEXPRESS;..."  // ❌ Single backslash
"Server=localhost\SQLEXPRESS;..."   // ✅ Hoặc không escape

// 2. SQL Server not running
// → Start SQL Server service

// 3. Firewall blocking
// → Allow port 1433

// 4. Wrong authentication
"User Id=sa;Password=wrong"  // ❌ Sai password

// 5. TrustServerCertificate required (SQL Server 2022+)
"Server=...;TrustServerCertificate=True"  // ✅ Thêm này
```

---

### Q: "Login failed for user"

**A:** Kiểm tra authentication:

**SQL Server Authentication:**

```csharp
"Server=.;Database=MyDB;User Id=sa;Password=YourPassword;TrustServerCertificate=True"
```

Đảm bảo:
- SQL Server Authentication enabled (Mixed Mode)
- User có permission trên database

**Windows Authentication:**

```csharp
"Server=.;Database=MyDB;Integrated Security=true;TrustServerCertificate=True"
```

---

### Q: Named connection không work

**A:** Kiểm tra:

**1. Connection string registered:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "...",
    "HISConnection": "...",      // ✅ Name phải có "Connection" suffix
    "LISConnection": "..."
  }
}
```

**2. Sử dụng đúng tên (không có suffix):**

```csharp
// ❌ Sai
using var conn = _sql.CreateConnection("HISConnection");  // Không thêm "Connection"

// ✅ Đúng
using var conn = _sql.CreateConnection("HIS");  // Framework tự remove suffix
```

---

## Query Execution Issues

### Q: Query returns null/empty khi có data

**A:** Kiểm tra:

**1. Property names match column names:**

```csharp
public class User
{
    public int Id { get; set; }           // ✅ Match SQL column "Id"
    public string UserName { get; set; }  // ❌ SQL column là "Username" không phải "UserName"
}

// Dapper case-insensitive by default nhưng tốt nhất nên match exact
```

**2. ResultClass trong XML (optional):**

```xml
<select id="User.GetAll" resultClass="User">  <!-- resultClass chỉ là hint, không bắt buộc -->
```

**3. SQL returns data:**

Test SQL trực tiếp trong SSMS để verify.

---

### Q: "Sequence contains no elements" exception

**A:** `StatementExecuteSingleAsync` throw exception nếu không có kết quả.

**Solutions:**

```csharp
// Option 1: Sử dụng FirstOrDefault
var user = await conn.StatementExecuteFirstAsync<User>("User.GetById", new { Id = 999 });
if (user == null) { ... }

// Option 2: Handle exception
try
{
    var user = await conn.StatementExecuteSingleAsync<User>("User.GetById", new { Id = 999 });
}
catch (InvalidOperationException)
{
    // No user found
}

// Option 3: Check count first
var count = await conn.StatementExecuteScalarAsync<int>("User.CountById", new { Id = 999 });
if (count > 0)
{
    var user = await conn.StatementExecuteSingleAsync<User>("User.GetById", new { Id = 999 });
}
```

---

### Q: DateTime mapping issues

**A:** SQL Server DateTime vs C# DateTime:

```csharp
// C# Model
public class User
{
    public DateTime CreatedAt { get; set; }      // DateTime (not null)
    public DateTime? UpdatedAt { get; set; }     // DateTime? (nullable)
}

// SQL
CREATE TABLE Users (
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NULL
)
```

**Common issues:**

```csharp
// 1. SQL returns NULL but C# property not nullable
public DateTime UpdatedAt { get; set; }  // ❌ Exception if SQL NULL

public DateTime? UpdatedAt { get; set; } // ✅ Nullable

// 2. Timezone issues
// SQL Server DATETIME không có timezone
// Sử dụng DATETIMEOFFSET nếu cần timezone
```

---

## Transaction Issues

### Q: Transaction không rollback khi có lỗi

**A:** Đảm bảo exception được throw:

```csharp
// ❌ Sai - Exception bị catch, không rollback
await conn.ExecuteInTransactionAsync(async c =>
{
    try
    {
        await c.StatementExecuteAsync("User.Insert", user);
        await c.StatementExecuteAsync("InvalidStatement", data);  // Lỗi
    }
    catch (Exception ex)
    {
        // Exception caught → Không throw → Commit xảy ra
        _logger.LogError(ex, "Error");
    }
});

// ✅ Đúng - Exception được throw
await conn.ExecuteInTransactionAsync(async c =>
{
    await c.StatementExecuteAsync("User.Insert", user);
    await c.StatementExecuteAsync("Order.Insert", order);
    // Exception tự động throw → Rollback
});

// ✅ Đúng - Log nhưng vẫn throw
try
{
    await conn.ExecuteInTransactionAsync(async c =>
    {
        await c.StatementExecuteAsync("User.Insert", user);
        await c.StatementExecuteAsync("Order.Insert", order);
    });
}
catch (Exception ex)
{
    _logger.LogError(ex, "Transaction failed");
    throw;  // Re-throw
}
```

---

### Q: Deadlock trong transaction

**A:** Best practices:

```csharp
// 1. Keep transactions short
await conn.ExecuteInTransactionAsync(async c =>
{
    // ❌ Sai - Quá nhiều operations
    await DoHeavyProcessing();
    await CallExternalAPI();
    await c.StatementExecuteAsync(...);
});

// ✅ Đúng - Chỉ database operations
await DoHeavyProcessing();
await CallExternalAPI();
await conn.ExecuteInTransactionAsync(async c =>
{
    await c.StatementExecuteAsync(...);
});

// 2. Consistent lock order
// Luôn lock tables theo thứ tự: Users → Orders → OrderItems

// 3. Use appropriate isolation level
// Default: READ COMMITTED (usually OK)
```

---

## Best Practices

### Q: Nên dùng pattern nào để register SQL Maps?

**A:** Recommendations:

**Small projects (<10 DAOs):**
```csharp
options.AutoDiscoverSqlMaps("SqlMaps");  // ✅ Đơn giản nhất
```

**Medium projects (10-50 DAOs):**
```csharp
var daoFiles = Provider.GetDaoFiles("SqlMaps");
options.ConfigureSqlMaps(provider =>
{
    foreach (var file in daoFiles.Where(File.Exists))
    {
        var name = Path.GetFileNameWithoutExtension(file);
        provider.AddFile(name, file, Provider.GetDescription(name));
    }
});
```

**Large projects (>50 DAOs) hoặc conditional registration:**
```csharp
options.ConfigureSqlMaps(provider =>
{
    // Core DAOs
    provider.AddFile("DAO000", "SqlMaps/DAO000.xml", "System");
    provider.AddFile("DAO001", "SqlMaps/DAO001.xml", "Users");

    // Feature-specific DAOs
    if (config.GetValue<bool>("Features:Reports"))
    {
        provider.AddFile("DAO100", "SqlMaps/DAO100.xml", "Reports");
    }
});
```

---

### Q: Repository hay ISql trực tiếp?

**A:**

**ISql trực tiếp (Simple services):**

```csharp
public class UserService
{
    private readonly ISql _sql;

    public async Task<User?> GetByIdAsync(int id)
    {
        _sql.GetDAO("DAO001");
        using var conn = _sql.CreateConnection();
        return await conn.StatementExecuteSingleAsync<User>("User.GetById", new { Id = id });
    }
}
```

**Pros:** Đơn giản, ít code
**Cons:** Duplicate DAO switching, connection creation

---

**Repository Pattern (Large projects):**

```csharp
public abstract class BaseRepository
{
    protected readonly ISql _sql;
    protected abstract string DaoName { get; }

    protected async Task<T?> QuerySingleAsync<T>(string statementId, object? parameters = null)
    {
        _sql.GetDAO(DaoName);
        using var conn = _sql.CreateConnection();
        return await conn.StatementExecuteSingleAsync<T>(statementId, parameters);
    }
}

public class UserRepository : BaseRepository
{
    protected override string DaoName => "DAO001";

    public Task<User?> GetByIdAsync(int id)
        => QuerySingleAsync<User>("User.GetById", new { Id = id });
}
```

**Pros:** Reusable, clean, testable
**Cons:** Thêm boilerplate code

---

### Q: Nên đặt tên statement IDs như thế nào?

**A:** Naming conventions:

```xml
<!-- ✅ Recommended: Namespace.Action[Entity] -->
<select id="User.GetById">
<select id="User.GetAll">
<select id="User.Search">
<insert id="User.Insert">
<update id="User.Update">
<delete id="User.Delete">

<select id="Order.GetByUserId">
<select id="Order.GetByDateRange">

<!-- ❌ Avoid -->
<select id="GetUserById">       <!-- Missing namespace -->
<select id="get_all_users">     <!-- Wrong case -->
<select id="UserGetAll">        <!-- Không rõ ràng -->
```

---

### Q: Logging SQL queries để debug?

**A:** Enable logging:

**appsettings.json:**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "WSC.DataAccess": "Debug"  // Enable debug logging
    }
  }
}
```

**Custom logging:**

```csharp
_logger.LogDebug("Executing query: {StatementId} with parameters: {@Parameters}",
    statementId,
    parameters);

var result = await conn.StatementExecuteQueryAsync<User>(statementId, parameters);

_logger.LogDebug("Query returned {Count} results", result.Count());
```

---

### Q: Testing với WSC.DataAccess?

**A:** Testing strategies:

**1. Integration testing (Recommended):**

```csharp
public class UserServiceTests : IClassFixture<DatabaseFixture>
{
    private readonly ISql _sql;

    [Fact]
    public async Task GetUserById_ShouldReturnUser()
    {
        // Arrange
        _sql.GetDAO("DAO001");
        using var conn = _sql.CreateConnection();

        // Act
        var user = await conn.StatementExecuteSingleAsync<User>("User.GetById", new { Id = 1 });

        // Assert
        Assert.NotNull(user);
        Assert.Equal(1, user.Id);
    }
}
```

**2. Mock ISql:**

```csharp
public class UserServiceTests
{
    [Fact]
    public async Task GetUserById_ShouldReturnUser()
    {
        // Arrange
        var mockSql = new Mock<ISql>();
        var mockConnection = new Mock<ISqlMapConnection>();

        mockSql.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);
        mockConnection
            .Setup(x => x.StatementExecuteSingleAsync<User>("User.GetById", It.IsAny<object>()))
            .ReturnsAsync(new User { Id = 1, Username = "test" });

        var service = new UserService(mockSql.Object);

        // Act
        var user = await service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(1, user.Id);
    }
}
```

---

## Troubleshooting Checklist

Khi gặp lỗi, check theo thứ tự:

1. ✅ Connection string đúng và database online?
2. ✅ SQL Map files tồn tại và được copy to output?
3. ✅ SQL Map files được register (AutoDiscoverSqlMaps hoặc manual)?
4. ✅ `GetDAO()` được gọi trước `CreateConnection()`?
5. ✅ Statement ID đúng (có namespace prefix)?
6. ✅ Parameters match (@Id, @Username, etc.)?
7. ✅ Model properties match SQL columns?
8. ✅ XML syntax đúng (có CDATA, đóng tags)?
9. ✅ Logging enabled để xem error details?

---

## Getting Help

Nếu vẫn gặp vấn đề:

1. **Check sample applications:** `samples/WSC.DataAccess.Sample/`
2. **Review documentation:**
   - [Getting Started](GETTING_STARTED.md)
   - [Configuration](CONFIGURATION.md)
   - [Examples](EXAMPLES.md)
   - [API Reference](API_REFERENCE.md)
3. **Enable debug logging** để xem chi tiết lỗi
4. **Create minimal reproduction** để dễ debug

---

## Common Error Messages

| Error | Cause | Solution |
|-------|-------|----------|
| "DAO 'XXX' not found" | SQL Map không registered | Check `AutoDiscoverSqlMaps()` hoặc manual registration |
| "Statement 'XXX' not found" | Statement ID sai hoặc XML parsing lỗi | Check statement ID và XML syntax |
| "Connection string not found" | Connection string không cấu hình | Check `appsettings.json` |
| "Cannot open database" | Database offline hoặc connection string sai | Check SQL Server service và connection string |
| "Sequence contains no elements" | Query không trả về kết quả | Use `FirstOrDefault` thay vì `Single` |
| "Login failed" | Authentication issues | Check credentials và SQL Server authentication mode |

---

## See Also

- [Getting Started](GETTING_STARTED.md) - Setup guide
- [Configuration](CONFIGURATION.md) - Configuration options
- [Examples](EXAMPLES.md) - Usage examples
- [API Reference](API_REFERENCE.md) - Complete API documentation
