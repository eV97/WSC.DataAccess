# Examples - WSC.DataAccess

Các ví dụ thực tế từ sample applications.

## Mục lục

- [Basic CRUD Operations](#basic-crud-operations)
- [Query Operations](#query-operations)
- [Transaction Examples](#transaction-examples)
- [Multiple Database Connections](#multiple-database-connections)
- [System Queries](#system-queries)
- [Cross-DAO Queries](#cross-dao-queries)
- [Repository Pattern](#repository-pattern)

---

## Basic CRUD Operations

### Get All Records

```csharp
public async Task<IEnumerable<User>> GetAllUsersAsync()
{
    _sql.GetDAO(Provider.DAO001);
    using var connection = _sql.CreateConnection();

    var users = await connection.StatementExecuteQueryAsync<User>("User.GetAllUsers");
    return users;
}
```

**SQL Map (DAO001.xml):**

```xml
<select id="User.GetAllUsers" resultClass="User">
  <![CDATA[
    SELECT Id, Username, FullName, Email, IsActive
    FROM Users
    WHERE IsActive = 1
    ORDER BY FullName
  ]]>
</select>
```

---

### Get Single Record by ID

```csharp
public async Task<User?> GetUserByIdAsync(int userId)
{
    _sql.GetDAO(Provider.DAO001);
    using var connection = _sql.CreateConnection();

    var user = await connection.StatementExecuteSingleAsync<User>(
        "User.GetUserById",
        new { Id = userId });

    return user;
}
```

**SQL Map:**

```xml
<select id="User.GetUserById" resultClass="User">
  <![CDATA[
    SELECT Id, Username, FullName, Email, IsActive
    FROM Users
    WHERE Id = @Id
  ]]>
</select>
```

---

### Insert Record

```csharp
public async Task<int> CreateUserAsync(User user)
{
    _sql.GetDAO(Provider.DAO001);
    using var connection = _sql.CreateConnection();

    var rowsAffected = await connection.StatementExecuteAsync(
        "User.InsertUser",
        user);

    return rowsAffected;
}
```

**SQL Map:**

```xml
<insert id="User.InsertUser">
  <![CDATA[
    INSERT INTO Users (Username, FullName, Email, PasswordHash, IsActive)
    VALUES (@Username, @FullName, @Email, @PasswordHash, @IsActive)
  ]]>
</insert>
```

---

### Update Record

```csharp
public async Task<int> UpdateUserAsync(User user)
{
    _sql.GetDAO(Provider.DAO001);
    using var connection = _sql.CreateConnection();

    var rowsAffected = await connection.StatementExecuteAsync(
        "User.UpdateUser",
        user);

    return rowsAffected;
}
```

**SQL Map:**

```xml
<update id="User.UpdateUser">
  <![CDATA[
    UPDATE Users
    SET FullName = @FullName,
        Email = @Email,
        IsActive = @IsActive,
        UpdatedAt = GETDATE()
    WHERE Id = @Id
  ]]>
</update>
```

---

### Delete Record (Soft Delete)

```csharp
public async Task<int> DeleteUserAsync(int userId)
{
    _sql.GetDAO(Provider.DAO001);
    using var connection = _sql.CreateConnection();

    var rowsAffected = await connection.StatementExecuteAsync(
        "User.DeleteUser",
        new { Id = userId });

    return rowsAffected;
}
```

**SQL Map:**

```xml
<update id="User.DeleteUser">
  <![CDATA[
    UPDATE Users
    SET IsActive = 0,
        UpdatedAt = GETDATE()
    WHERE Id = @Id
  ]]>
</update>
```

---

## Query Operations

### Query with Parameters

```csharp
public async Task<IEnumerable<User>> SearchUsersByNameAsync(string searchTerm)
{
    _sql.GetDAO(Provider.DAO001);
    using var connection = _sql.CreateConnection();

    var users = await connection.StatementExecuteQueryAsync<User>(
        "User.SearchByName",
        new { SearchTerm = $"%{searchTerm}%" });

    return users;
}
```

**SQL Map:**

```xml
<select id="User.SearchByName" resultClass="User">
  <![CDATA[
    SELECT Id, Username, FullName, Email, IsActive
    FROM Users
    WHERE FullName LIKE @SearchTerm
    AND IsActive = 1
    ORDER BY FullName
  ]]>
</select>
```

---

### Scalar Query (Count, Sum, etc.)

```csharp
public async Task<int> CountUsersAsync()
{
    _sql.GetDAO(Provider.DAO001);
    using var connection = _sql.CreateConnection();

    var count = await connection.StatementExecuteScalarAsync<int>("User.CountUsers");
    return count;
}
```

**SQL Map:**

```xml
<select id="User.CountUsers" resultClass="int">
  <![CDATA[
    SELECT COUNT(*)
    FROM Users
    WHERE IsActive = 1
  ]]>
</select>
```

---

### Complex Query with Multiple Parameters

```csharp
public async Task<IEnumerable<Order>> GetOrdersByDateRangeAsync(
    DateTime startDate,
    DateTime endDate,
    string status)
{
    _sql.GetDAO(Provider.DAO003);
    using var connection = _sql.CreateConnection();

    var orders = await connection.StatementExecuteQueryAsync<Order>(
        "Order.GetByDateRange",
        new
        {
            StartDate = startDate,
            EndDate = endDate,
            Status = status
        });

    return orders;
}
```

**SQL Map:**

```xml
<select id="Order.GetByDateRange" resultClass="Order">
  <![CDATA[
    SELECT Id, OrderNumber, CustomerId, OrderDate, TotalAmount, Status
    FROM Orders
    WHERE OrderDate >= @StartDate
    AND OrderDate <= @EndDate
    AND Status = @Status
    ORDER BY OrderDate DESC
  ]]>
</select>
```

---

## Transaction Examples

### Simple Transaction

```csharp
public async Task CreateOrderWithItemsAsync(Order order, List<OrderItem> items)
{
    _sql.GetDAO(Provider.DAO003);
    using var connection = _sql.CreateConnection();

    await connection.ExecuteInTransactionAsync(async conn =>
    {
        // Insert order
        await conn.StatementExecuteAsync("Order.Insert", order);

        // Insert order items
        foreach (var item in items)
        {
            item.OrderId = order.Id;
            await conn.StatementExecuteAsync("OrderItem.Insert", item);
        }

        // Transaction auto-commits if no exception
    });
}
```

---

### Transaction with Multiple DAOs

```csharp
public async Task TransferUserDataAsync(int userId)
{
    _sql.GetDAO(Provider.DAO001);
    using var connection = _sql.CreateConnection();

    await connection.ExecuteInTransactionAsync(async conn =>
    {
        // Insert user 1
        var user1 = new User
        {
            Username = $"user1_{DateTime.Now:HHmmss}",
            FullName = "User One",
            Email = "user1@example.com",
            PasswordHash = "hash123",
            IsActive = true
        };
        await conn.StatementExecuteAsync("User.InsertUser", user1);

        // Insert user 2
        var user2 = new User
        {
            Username = $"user2_{DateTime.Now:HHmmss}",
            FullName = "User Two",
            Email = "user2@example.com",
            PasswordHash = "hash123",
            IsActive = true
        };
        await conn.StatementExecuteAsync("User.InsertUser", user2);

        // If any insert fails, all changes are rolled back
    });
}
```

**Sample:** See `samples/WSC.DataAccess.Sample/TestService.cs:204` (TestTransactionAsync)

---

### Transaction with Error Handling

```csharp
public async Task<bool> ProcessOrderAsync(Order order)
{
    try
    {
        _sql.GetDAO(Provider.DAO003);
        using var connection = _sql.CreateConnection();

        await connection.ExecuteInTransactionAsync(async conn =>
        {
            // Step 1: Insert order
            await conn.StatementExecuteAsync("Order.Insert", order);

            // Step 2: Update inventory
            await conn.StatementExecuteAsync("Inventory.UpdateStock", order);

            // Step 3: Create invoice
            await conn.StatementExecuteAsync("Invoice.Create", order);

            // If any step fails, entire transaction is rolled back
        });

        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to process order {OrderId}", order.Id);
        return false;
    }
}
```

---

## Multiple Database Connections

### Query from Multiple Databases

```csharp
public async Task TestNamedConnectionsAsync()
{
    _sql.GetDAO(Provider.DAO000);

    // Query from Default database
    using (var defaultConn = _sql.CreateConnection())
    {
        var dbName = await defaultConn.StatementExecuteSingleAsync<string>(
            "System.GetCurrentDatabase");
        _logger.LogInformation("Default DB: {DbName}", dbName);
    }

    // Query from HIS database
    using (var hisConn = _sql.CreateConnection("HIS"))
    {
        var dbName = await hisConn.StatementExecuteSingleAsync<string>(
            "System.GetCurrentDatabase");
        _logger.LogInformation("HIS DB: {DbName}", dbName);
    }

    // Query from LIS database
    using (var lisConn = _sql.CreateConnection("LIS"))
    {
        var dbName = await lisConn.StatementExecuteSingleAsync<string>(
            "System.GetCurrentDatabase");
        _logger.LogInformation("LIS DB: {DbName}", dbName);
    }
}
```

**appsettings.json:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=AppDB;...",
    "HISConnection": "Server=.;Database=HIS;...",
    "LISConnection": "Server=.;Database=LIS;..."
  }
}
```

**Sample:** See `samples/WSC.DataAccess.Sample/TestService.cs:293` (TestNamedConnectionsAsync)

---

### Cross-Database Query

```csharp
public async Task<PatientReportDTO> GetPatientReportAsync(int userId)
{
    _sql.GetDAO(Provider.DAO001);

    // Get user from Application DB
    using var appConn = _sql.CreateConnection();
    var user = await appConn.StatementExecuteSingleAsync<User>(
        "User.GetUserById",
        new { Id = userId });

    // Get patient data from HIS DB
    using var hisConn = _sql.CreateConnection("HIS");
    var patient = await hisConn.StatementExecuteSingleAsync<Patient>(
        "Patient.GetByUserId",
        new { UserId = userId });

    // Get lab results from LIS DB
    using var lisConn = _sql.CreateConnection("LIS");
    var labResults = await lisConn.StatementExecuteQueryAsync<LabResult>(
        "LabResult.GetByPatientId",
        new { PatientId = patient.Id });

    return new PatientReportDTO
    {
        User = user,
        Patient = patient,
        LabResults = labResults.ToList()
    };
}
```

**Sample:** See `samples/WSC.DataAccess.Sample/TestService.cs:156` (TestCrossDaoQueryAsync)

---

## System Queries

### Test Database Connection

```csharp
public async Task<bool> TestConnectionAsync()
{
    _sql.GetDAO(Provider.DAO000);
    using var connection = _sql.CreateConnection();

    var result = await connection.StatementExecuteScalarAsync<int>(
        "System.TestConnection");

    return result == 1;
}
```

**SQL Map (DAO000.xml):**

```xml
<select id="System.TestConnection" resultClass="int">
  <![CDATA[
    SELECT 1
  ]]>
</select>
```

---

### Get Database Information

```csharp
public async Task<DatabaseInfo> GetDatabaseInfoAsync()
{
    _sql.GetDAO(Provider.DAO000);
    using var connection = _sql.CreateConnection();

    var info = new DatabaseInfo
    {
        Version = await connection.StatementExecuteSingleAsync<string>(
            "System.GetDatabaseVersion"),

        DatabaseName = await connection.StatementExecuteSingleAsync<string>(
            "System.GetCurrentDatabase"),

        ServerName = await connection.StatementExecuteSingleAsync<string>(
            "System.GetServerName"),

        CurrentTime = await connection.StatementExecuteSingleAsync<DateTime>(
            "System.GetCurrentDateTime")
    };

    return info;
}
```

**SQL Map:**

```xml
<select id="System.GetDatabaseVersion" resultClass="string">
  <![CDATA[SELECT @@VERSION]]>
</select>

<select id="System.GetCurrentDatabase" resultClass="string">
  <![CDATA[SELECT DB_NAME()]]>
</select>

<select id="System.GetServerName" resultClass="string">
  <![CDATA[SELECT @@SERVERNAME]]>
</select>

<select id="System.GetCurrentDateTime" resultClass="DateTime">
  <![CDATA[SELECT GETDATE()]]>
</select>
```

**Sample:** See `samples/WSC.DataAccess.Sample/TestService.cs:252` (TestSystemQueriesAsync)

---

## Cross-DAO Queries

### Query from Different DAOs

```csharp
public async Task TestCrossDaoQueryAsync(int userId)
{
    // Get user from DAO001
    _sql.GetDAO(Provider.DAO001);
    using var userConn = _sql.CreateConnection();
    var user = await userConn.StatementExecuteSingleAsync<User>(
        "User.GetUserById",
        new { Id = userId });

    if (user == null) return;

    _logger.LogInformation("User: {FullName}", user.FullName);

    // Get products from DAO002
    _sql.GetDAO(Provider.DAO002);
    using var productConn = _sql.CreateConnection();
    var products = await productConn.StatementExecuteQueryAsync<Product>(
        "Product.GetAllProducts");

    _logger.LogInformation("Found {Count} products", products.Count());
}
```

**Sample:** See `samples/WSC.DataAccess.Sample/TestService.cs:156`

---

## Repository Pattern

### Base Repository

```csharp
public abstract class BaseRepository
{
    protected readonly ISql _sql;
    protected readonly ILogger _logger;
    protected abstract string DaoName { get; }

    protected BaseRepository(ISql sql, ILogger logger)
    {
        _sql = sql;
        _logger = logger;
    }

    protected async Task<IEnumerable<T>> QueryAsync<T>(
        string statementId,
        object? parameters = null)
    {
        _sql.GetDAO(DaoName);
        using var connection = _sql.CreateConnection();
        return await connection.StatementExecuteQueryAsync<T>(statementId, parameters);
    }

    protected async Task<T?> QuerySingleAsync<T>(
        string statementId,
        object? parameters = null)
    {
        _sql.GetDAO(DaoName);
        using var connection = _sql.CreateConnection();
        return await connection.StatementExecuteSingleAsync<T>(statementId, parameters);
    }

    protected async Task<int> ExecuteAsync(
        string statementId,
        object? parameters = null)
    {
        _sql.GetDAO(DaoName);
        using var connection = _sql.CreateConnection();
        return await connection.StatementExecuteAsync(statementId, parameters);
    }
}
```

---

### User Repository

```csharp
public class UserRepository : BaseRepository
{
    protected override string DaoName => Provider.DAO001;

    public UserRepository(ISql sql, ILogger<UserRepository> logger)
        : base(sql, logger)
    {
    }

    public Task<IEnumerable<User>> GetAllAsync()
        => QueryAsync<User>("User.GetAllUsers");

    public Task<User?> GetByIdAsync(int id)
        => QuerySingleAsync<User>("User.GetUserById", new { Id = id });

    public Task<User?> GetByUsernameAsync(string username)
        => QuerySingleAsync<User>("User.GetUserByUsername", new { Username = username });

    public Task<int> CountAsync()
        => QuerySingleAsync<int>("User.CountUsers") ?? Task.FromResult(0);

    public Task<int> InsertAsync(User user)
        => ExecuteAsync("User.InsertUser", user);

    public Task<int> UpdateAsync(User user)
        => ExecuteAsync("User.UpdateUser", user);

    public Task<int> DeleteAsync(int id)
        => ExecuteAsync("User.DeleteUser", new { Id = id });
}
```

---

### Using Repository

```csharp
public class UserService
{
    private readonly UserRepository _userRepo;

    public UserService(UserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _userRepo.GetAllAsync();
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _userRepo.GetByIdAsync(id);
    }

    public async Task<bool> CreateUserAsync(User user)
    {
        var rowsAffected = await _userRepo.InsertAsync(user);
        return rowsAffected > 0;
    }
}
```

---

## Complete Example: Full CRUD Service

```csharp
public class ProductService
{
    private readonly ISql _sql;
    private readonly ILogger<ProductService> _logger;

    public ProductService(ISql sql, ILogger<ProductService> logger)
    {
        _sql = sql;
        _logger = logger;
    }

    public async Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        _sql.GetDAO(Provider.DAO002);
        using var conn = _sql.CreateConnection();
        return await conn.StatementExecuteQueryAsync<Product>("Product.GetAllProducts");
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        _sql.GetDAO(Provider.DAO002);
        using var conn = _sql.CreateConnection();
        return await conn.StatementExecuteSingleAsync<Product>(
            "Product.GetById",
            new { Id = id });
    }

    public async Task<IEnumerable<Product>> SearchProductsAsync(string keyword)
    {
        _sql.GetDAO(Provider.DAO002);
        using var conn = _sql.CreateConnection();
        return await conn.StatementExecuteQueryAsync<Product>(
            "Product.Search",
            new { Keyword = $"%{keyword}%" });
    }

    public async Task<bool> CreateProductAsync(Product product)
    {
        _sql.GetDAO(Provider.DAO002);
        using var conn = _sql.CreateConnection();

        var rowsAffected = await conn.StatementExecuteAsync(
            "Product.Insert",
            product);

        return rowsAffected > 0;
    }

    public async Task<bool> UpdateProductAsync(Product product)
    {
        _sql.GetDAO(Provider.DAO002);
        using var conn = _sql.CreateConnection();

        var rowsAffected = await conn.StatementExecuteAsync(
            "Product.Update",
            product);

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        _sql.GetDAO(Provider.DAO002);
        using var conn = _sql.CreateConnection();

        var rowsAffected = await conn.StatementExecuteAsync(
            "Product.Delete",
            new { Id = id });

        return rowsAffected > 0;
    }

    public async Task<decimal> GetTotalInventoryValueAsync()
    {
        _sql.GetDAO(Provider.DAO002);
        using var conn = _sql.CreateConnection();

        var total = await conn.StatementExecuteScalarAsync<decimal>(
            "Product.GetTotalValue");

        return total;
    }
}
```

---

## See Also

- [API Reference](API_REFERENCE.md) - Complete API documentation
- [Configuration](CONFIGURATION.md) - Configuration options
- [Getting Started](GETTING_STARTED.md) - Setup guide
- [FAQ](FAQ.md) - Common questions and troubleshooting

## Sample Applications

Xem sample code đầy đủ tại:
- `samples/WSC.DataAccess.Sample/TestService.cs` - Complete test suite
- `samples/WSC.DataAccess.Sample/Program.cs` - Configuration examples
- `samples/WSC.DataAccess.Sample/SqlMaps/` - SQL Map file examples
