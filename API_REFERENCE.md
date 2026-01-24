# API Reference - WSC.DataAccess

Tài liệu API đầy đủ cho WSC.DataAccess library.

## Mục lục

- [Core Interfaces](#core-interfaces)
  - [ISql](#isql)
  - [ISqlMapConnection](#isqlmapconnection)
- [Extension Methods](#extension-methods)
  - [Query Methods](#query-methods)
  - [Execute Methods](#execute-methods)
  - [Transaction Methods](#transaction-methods)
- [Configuration](#configuration)
  - [AddWscDataAccess](#addwscdataaccess)
  - [SqlMapOptions](#sqlmapoptions)
- [Provider Pattern](#provider-pattern)

---

## Core Interfaces

### ISql

Interface chính để tương tác với database - Cung cấp DAO switching và connection management.

**Namespace:** `WSC.DataAccess.Core`

#### Methods

##### CreateConnection()

Tạo connection mới với default connection string.

```csharp
ISqlMapConnection CreateConnection()
```

**Returns:** `ISqlMapConnection` - Database connection với SQL map capabilities

**Example:**

```csharp
_sql.GetDAO("DAO001");
using var connection = _sql.CreateConnection();
```

---

##### CreateConnection(string connectionName)

Tạo connection mới với named connection string.

```csharp
ISqlMapConnection CreateConnection(string connectionName)
```

**Parameters:**
- `connectionName` (string) - Tên connection (e.g., "HIS", "LIS", "Report")

**Returns:** `ISqlMapConnection` - Database connection

**Example:**

```csharp
_sql.GetDAO("DAO001");
using var hisConnection = _sql.CreateConnection("HIS");
using var lisConnection = _sql.CreateConnection("LIS");
```

---

##### GetDAO(string daoName)

Chuyển đổi DAO context - Tất cả statements sau đó sẽ sử dụng SQL map của DAO này.

```csharp
void GetDAO(string daoName)
```

**Parameters:**
- `daoName` (string) - Tên DAO (e.g., "DAO000", "DAO001")

**Example:**

```csharp
_sql.GetDAO("DAO001");  // Switch to User DAO
_sql.GetDAO("DAO002");  // Switch to Product DAO
```

---

##### GetDAO(string daoName, string connectionName)

Chuyển đổi DAO context cho specific connection.

```csharp
void GetDAO(string daoName, string connectionName)
```

**Parameters:**
- `daoName` (string) - Tên DAO
- `connectionName` (string) - Tên connection

**Example:**

```csharp
_sql.GetDAO("DAO001", "HIS");
```

---

#### Properties

##### CurrentDao

Lấy tên DAO hiện đang active.

```csharp
string? CurrentDao { get; }
```

**Example:**

```csharp
_sql.GetDAO("DAO001");
Console.WriteLine(_sql.CurrentDao);  // Output: "DAO001"
```

---

##### CurrentConnection

Lấy tên connection hiện đang active.

```csharp
string CurrentConnection { get; }
```

**Example:**

```csharp
using var conn = _sql.CreateConnection("HIS");
Console.WriteLine(_sql.CurrentConnection);  // Output: "HIS"
```

---

### ISqlMapConnection

Interface cho database connection với SQL map support - Extends `IDbConnection`.

**Namespace:** `WSC.DataAccess.Core`

#### Properties

##### SqlMapConfig

Lấy SQL map configuration cho connection này.

```csharp
SqlMapConfig SqlMapConfig { get; }
```

---

##### InnerConnection

Lấy underlying raw database connection.

```csharp
IDbConnection InnerConnection { get; }
```

---

##### DaoName

Lấy tên DAO được associate với connection này.

```csharp
string? DaoName { get; }
```

---

##### ConnectionName

Lấy tên connection.

```csharp
string ConnectionName { get; }
```

---

## Extension Methods

Extension methods cho `ISqlMapConnection` - Cung cấp iBatis.NET style API.

**Namespace:** `WSC.DataAccess.Extensions`

### Query Methods

#### StatementExecuteQueryAsync<T>

Execute query statement và trả về danh sách kết quả.

```csharp
Task<IEnumerable<T>> StatementExecuteQueryAsync<T>(
    this ISqlMapConnection connection,
    string statementId,
    object? parameters = null)
```

**Type Parameters:**
- `T` - Type của result object

**Parameters:**
- `connection` (ISqlMapConnection) - SQL connection
- `statementId` (string) - Statement ID từ SQL map (e.g., "User.GetAll")
- `parameters` (object?, optional) - Query parameters (anonymous object hoặc dictionary)

**Returns:** `Task<IEnumerable<T>>` - Danh sách kết quả

**Throws:**
- `ArgumentNullException` - Nếu connection null
- `ArgumentException` - Nếu statementId null hoặc empty
- `InvalidOperationException` - Nếu statement không tìm thấy trong SQL map

**Example:**

```csharp
_sql.GetDAO("DAO001");
using var conn = _sql.CreateConnection();

// No parameters
var allUsers = await conn.StatementExecuteQueryAsync<User>("User.GetAll");

// With parameters
var activeUsers = await conn.StatementExecuteQueryAsync<User>(
    "User.GetByStatus",
    new { IsActive = true });

// With search parameter
var searchResults = await conn.StatementExecuteQueryAsync<User>(
    "User.Search",
    new { Keyword = "%john%" });
```

---

#### StatementExecuteSingleAsync<T>

Execute query statement và trả về 1 kết quả duy nhất (hoặc null).

```csharp
Task<T?> StatementExecuteSingleAsync<T>(
    this ISqlMapConnection connection,
    string statementId,
    object? parameters = null)
```

**Type Parameters:**
- `T` - Type của result object

**Parameters:**
- `connection` (ISqlMapConnection) - SQL connection
- `statementId` (string) - Statement ID từ SQL map
- `parameters` (object?, optional) - Query parameters

**Returns:** `Task<T?>` - Single result hoặc null nếu không tìm thấy

**Throws:**
- `InvalidOperationException` - Nếu query trả về nhiều hơn 1 record

**Example:**

```csharp
_sql.GetDAO("DAO001");
using var conn = _sql.CreateConnection();

var user = await conn.StatementExecuteSingleAsync<User>(
    "User.GetById",
    new { Id = 123 });

if (user != null)
{
    Console.WriteLine($"Found: {user.Username}");
}
```

---

#### StatementExecuteFirstAsync<T>

Execute query statement và trả về kết quả đầu tiên (hoặc null).

```csharp
Task<T?> StatementExecuteFirstAsync<T>(
    this ISqlMapConnection connection,
    string statementId,
    object? parameters = null)
```

**Type Parameters:**
- `T` - Type của result object

**Parameters:**
- `connection` (ISqlMapConnection) - SQL connection
- `statementId` (string) - Statement ID từ SQL map
- `parameters` (object?, optional) - Query parameters

**Returns:** `Task<T?>` - First result hoặc null

**Difference from StatementExecuteSingleAsync:**
- `StatementExecuteSingleAsync` - Throws exception nếu có nhiều hơn 1 record
- `StatementExecuteFirstAsync` - Trả về record đầu tiên, bỏ qua các records khác

**Example:**

```csharp
_sql.GetDAO("DAO001");
using var conn = _sql.CreateConnection();

// Get first active user (không quan tâm có bao nhiêu)
var firstUser = await conn.StatementExecuteFirstAsync<User>(
    "User.GetActive");
```

---

#### StatementExecuteScalarAsync<T>

Execute scalar statement và trả về 1 giá trị đơn (COUNT, SUM, MAX, etc.).

```csharp
Task<T> StatementExecuteScalarAsync<T>(
    this ISqlMapConnection connection,
    string statementId,
    object? parameters = null)
```

**Type Parameters:**
- `T` - Type của scalar value (int, decimal, string, DateTime, etc.)

**Parameters:**
- `connection` (ISqlMapConnection) - SQL connection
- `statementId` (string) - Statement ID từ SQL map
- `parameters` (object?, optional) - Query parameters

**Returns:** `Task<T>` - Scalar value

**Example:**

```csharp
_sql.GetDAO("DAO001");
using var conn = _sql.CreateConnection();

// Count
var userCount = await conn.StatementExecuteScalarAsync<int>("User.Count");

// Sum
var totalAmount = await conn.StatementExecuteScalarAsync<decimal>("Order.GetTotalAmount");

// Max
var maxId = await conn.StatementExecuteScalarAsync<int>("User.GetMaxId");

// String
var dbName = await conn.StatementExecuteScalarAsync<string>("System.GetDatabaseName");

// DateTime
var serverTime = await conn.StatementExecuteScalarAsync<DateTime>("System.GetServerTime");
```

---

### Execute Methods

#### StatementExecuteAsync

Execute non-query statement (INSERT, UPDATE, DELETE).

```csharp
Task<int> StatementExecuteAsync(
    this ISqlMapConnection connection,
    string statementId,
    object? parameters = null)
```

**Parameters:**
- `connection` (ISqlMapConnection) - SQL connection
- `statementId` (string) - Statement ID từ SQL map
- `parameters` (object?, optional) - Query parameters

**Returns:** `Task<int>` - Số rows bị affected

**Example:**

```csharp
_sql.GetDAO("DAO001");
using var conn = _sql.CreateConnection();

// Insert
var newUser = new User
{
    Username = "john.doe",
    FullName = "John Doe",
    Email = "john@example.com"
};
var insertedRows = await conn.StatementExecuteAsync("User.Insert", newUser);

// Update
var rowsUpdated = await conn.StatementExecuteAsync(
    "User.Update",
    new { Id = 123, FullName = "Jane Doe", Email = "jane@example.com" });

// Delete
var rowsDeleted = await conn.StatementExecuteAsync(
    "User.Delete",
    new { Id = 123 });
```

---

### Transaction Methods

#### ExecuteInTransactionAsync

Execute multiple statements trong 1 transaction.

```csharp
Task ExecuteInTransactionAsync(
    this ISqlMapConnection connection,
    Func<ISqlMapConnection, Task> action)
```

**Parameters:**
- `connection` (ISqlMapConnection) - SQL connection
- `action` (Func<ISqlMapConnection, Task>) - Action để execute trong transaction

**Behavior:**
- Tự động **COMMIT** nếu không có exception
- Tự động **ROLLBACK** nếu có exception

**Example:**

```csharp
_sql.GetDAO("DAO003");
using var conn = _sql.CreateConnection();

await conn.ExecuteInTransactionAsync(async c =>
{
    // Insert order
    await c.StatementExecuteAsync("Order.Insert", order);

    // Insert order items
    foreach (var item in orderItems)
    {
        await c.StatementExecuteAsync("OrderItem.Insert", item);
    }

    // Update inventory
    await c.StatementExecuteAsync("Inventory.UpdateStock", order);

    // Tất cả thành công -> Auto COMMIT
    // Có lỗi -> Auto ROLLBACK
});
```

---

#### ExecuteInTransactionAsync<TResult>

Execute multiple statements trong transaction và trả về result.

```csharp
Task<TResult> ExecuteInTransactionAsync<TResult>(
    this ISqlMapConnection connection,
    Func<ISqlMapConnection, Task<TResult>> func)
```

**Type Parameters:**
- `TResult` - Type của kết quả transaction

**Parameters:**
- `connection` (ISqlMapConnection) - SQL connection
- `func` (Func<ISqlMapConnection, Task<TResult>>) - Function để execute

**Returns:** `Task<TResult>` - Transaction result

**Example:**

```csharp
_sql.GetDAO("DAO001");
using var conn = _sql.CreateConnection();

var newUserId = await conn.ExecuteInTransactionAsync(async c =>
{
    // Insert user
    await c.StatementExecuteAsync("User.Insert", newUser);

    // Get the new user ID
    var userId = await c.StatementExecuteScalarAsync<int>("User.GetLastInsertId");

    // Insert user permissions
    await c.StatementExecuteAsync("Permission.Insert", new { UserId = userId });

    return userId;  // Return new user ID
});

Console.WriteLine($"Created user with ID: {newUserId}");
```

---

## Configuration

### AddWscDataAccess

Extension methods để đăng ký WSC.DataAccess vào DI container.

**Namespace:** `WSC.DataAccess.Configuration`

#### Overload 1: IConfiguration

```csharp
IServiceCollection AddWscDataAccess(
    this IServiceCollection services,
    IConfiguration configuration,
    Action<SqlMapOptions>? configure = null)
```

**Parameters:**
- `services` (IServiceCollection) - Service collection
- `configuration` (IConfiguration) - Configuration object chứa ConnectionStrings section
- `configure` (Action<SqlMapOptions>?, optional) - Configuration action

**Connection String Mapping:**
- `DefaultConnection` → `"Default"`
- `HISConnection` → `"HIS"`
- `LISConnection` → `"LIS"`
- Suffix "Connection" tự động được remove

**Example:**

```csharp
builder.Services.AddWscDataAccess(
    builder.Configuration,
    options =>
    {
        options.AutoDiscoverSqlMaps("SqlMaps");
    });
```

---

#### Overload 2: IConfigurationSection

```csharp
IServiceCollection AddWscDataAccess(
    this IServiceCollection services,
    IConfigurationSection connectionStringsSection,
    Action<SqlMapOptions>? configure = null)
```

**Parameters:**
- `services` (IServiceCollection) - Service collection
- `connectionStringsSection` (IConfigurationSection) - ConnectionStrings section
- `configure` (Action<SqlMapOptions>?, optional) - Configuration action

**Example:**

```csharp
var connectionStringsSection = builder.Configuration.GetSection("ConnectionStrings");
builder.Services.AddWscDataAccess(
    connectionStringsSection,
    options =>
    {
        options.AutoDiscoverSqlMaps("SqlMaps");
    });
```

---

#### Overload 3: Dictionary<string, string>

```csharp
IServiceCollection AddWscDataAccess(
    this IServiceCollection services,
    Dictionary<string, string> connectionStrings,
    string defaultConnectionName,
    Action<SqlMapOptions>? configure = null)
```

**Parameters:**
- `services` (IServiceCollection) - Service collection
- `connectionStrings` (Dictionary<string, string>) - Connection strings dictionary
- `defaultConnectionName` (string) - Default connection name
- `configure` (Action<SqlMapOptions>?, optional) - Configuration action

**Example:**

```csharp
var connectionStrings = new Dictionary<string, string>
{
    { "Default", "Server=...;Database=DB1;..." },
    { "HIS", "Server=...;Database=HIS;..." },
    { "LIS", "Server=...;Database=LIS;..." }
};

builder.Services.AddWscDataAccess(
    connectionStrings,
    defaultConnectionName: "Default",
    options =>
    {
        options.AutoDiscoverSqlMaps("SqlMaps");
    });
```

---

#### Overload 4: Single Connection String

```csharp
IServiceCollection AddWscDataAccess(
    this IServiceCollection services,
    string connectionString,
    Action<SqlMapOptions>? configure = null)
```

**Parameters:**
- `services` (IServiceCollection) - Service collection
- `connectionString` (string) - Single connection string
- `configure` (Action<SqlMapOptions>?, optional) - Configuration action

**Example:**

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddWscDataAccess(
    connectionString,
    options =>
    {
        options.AutoDiscoverSqlMaps("SqlMaps");
    });
```

---

### SqlMapOptions

Options class để cấu hình SQL Map registration.

**Namespace:** `WSC.DataAccess.Configuration`

#### Methods

##### AutoDiscoverSqlMaps

Tự động scan và đăng ký tất cả .xml files trong directory.

```csharp
void AutoDiscoverSqlMaps(string baseDirectory)
```

**Parameters:**
- `baseDirectory` (string) - Thư mục chứa SQL Map files (e.g., "SqlMaps")

**Behavior:**
- Scan tất cả `*.xml` files trong directory
- Tự động register với name = filename (không extension)
- File `DAO001.xml` → DAO name = `"DAO001"`

**Example:**

```csharp
options.AutoDiscoverSqlMaps("SqlMaps");
```

---

##### ConfigureSqlMaps

Cấu hình manual SQL Map registration.

```csharp
void ConfigureSqlMaps(Action<SqlMapProvider> configure)
```

**Parameters:**
- `configure` (Action<SqlMapProvider>) - Configuration action

**Example:**

```csharp
options.ConfigureSqlMaps(provider =>
{
    provider.AddFile("DAO000", "SqlMaps/DAO000.xml", "System queries");
    provider.AddFile("DAO001", "SqlMaps/DAO001.xml", "User management");
    provider.AddFile("DAO002", "SqlMaps/DAO002.xml", "Product management");
});
```

---

#### SqlMapProvider Methods

##### AddFile

Đăng ký 1 SQL Map file.

```csharp
void AddFile(string daoName, string filePath, string? description = null)
```

**Parameters:**
- `daoName` (string) - DAO name (e.g., "DAO001")
- `filePath` (string) - Đường dẫn đến XML file
- `description` (string?, optional) - Mô tả của DAO

**Example:**

```csharp
provider.AddFile("DAO001", "SqlMaps/DAO001.xml", "User management");
```

---

## Provider Pattern

Static class để quản lý DAO names - Pattern phổ biến trong legacy projects.

**Namespace:** `YourProject.Models` (User-defined)

### Example Implementation

```csharp
public static class Provider
{
    // DAO Name Constants
    public const string DAO000 = "DAO000"; // System
    public const string DAO001 = "DAO001"; // Users
    public const string DAO002 = "DAO002"; // Products
    public const string DAO003 = "DAO003"; // Orders

    /// <summary>
    /// Get all DAO names
    /// </summary>
    public static string[] GetAllDaoNames()
    {
        return new[] { DAO000, DAO001, DAO002, DAO003 };
    }

    /// <summary>
    /// Get DAO file paths
    /// </summary>
    public static string[] GetDaoFiles(string baseDirectory)
    {
        return GetAllDaoNames()
            .Select(dao => Path.Combine(baseDirectory, $"{dao}.xml"))
            .ToArray();
    }

    /// <summary>
    /// Get DAO description
    /// </summary>
    public static string GetDescription(string daoName)
    {
        var descriptions = new Dictionary<string, string>
        {
            { DAO000, "System queries" },
            { DAO001, "User management" },
            { DAO002, "Product management" },
            { DAO003, "Order management" }
        };

        return descriptions.TryGetValue(daoName, out var desc)
            ? desc
            : "Unknown DAO";
    }
}
```

### Usage

```csharp
// In service
_sql.GetDAO(Provider.DAO001);

// In configuration
var daoFiles = Provider.GetDaoFiles("SqlMaps");
options.ConfigureSqlMaps(provider =>
{
    foreach (var file in daoFiles.Where(File.Exists))
    {
        var fileName = Path.GetFileNameWithoutExtension(file);
        var description = Provider.GetDescription(fileName);
        provider.AddFile(fileName, file, description);
    }
});
```

---

## Common Usage Patterns

### Pattern 1: Simple Query

```csharp
public async Task<IEnumerable<User>> GetAllUsersAsync()
{
    _sql.GetDAO("DAO001");
    using var conn = _sql.CreateConnection();
    return await conn.StatementExecuteQueryAsync<User>("User.GetAll");
}
```

---

### Pattern 2: Query with Parameters

```csharp
public async Task<User?> GetUserByIdAsync(int id)
{
    _sql.GetDAO("DAO001");
    using var conn = _sql.CreateConnection();
    return await conn.StatementExecuteSingleAsync<User>(
        "User.GetById",
        new { Id = id });
}
```

---

### Pattern 3: Insert/Update/Delete

```csharp
public async Task<bool> UpdateUserAsync(User user)
{
    _sql.GetDAO("DAO001");
    using var conn = _sql.CreateConnection();
    var rows = await conn.StatementExecuteAsync("User.Update", user);
    return rows > 0;
}
```

---

### Pattern 4: Transaction

```csharp
public async Task CreateOrderAsync(Order order, List<OrderItem> items)
{
    _sql.GetDAO("DAO003");
    using var conn = _sql.CreateConnection();

    await conn.ExecuteInTransactionAsync(async c =>
    {
        await c.StatementExecuteAsync("Order.Insert", order);
        foreach (var item in items)
        {
            await c.StatementExecuteAsync("OrderItem.Insert", item);
        }
    });
}
```

---

### Pattern 5: Multiple Databases

```csharp
public async Task<CombinedData> GetCombinedDataAsync(int id)
{
    _sql.GetDAO("DAO001");

    using var conn1 = _sql.CreateConnection();
    var user = await conn1.StatementExecuteSingleAsync<User>("User.GetById", new { Id = id });

    using var conn2 = _sql.CreateConnection("HIS");
    var patient = await conn2.StatementExecuteSingleAsync<Patient>("Patient.GetByUserId", new { UserId = id });

    return new CombinedData { User = user, Patient = patient };
}
```

---

## See Also

- [Examples](EXAMPLES.md) - Practical examples
- [Configuration](CONFIGURATION.md) - Configuration guide
- [Getting Started](GETTING_STARTED.md) - Quick start guide
- [FAQ](FAQ.md) - Common questions
