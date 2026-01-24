# Getting Started - WSC.DataAccess

Hướng dẫn từng bước để bắt đầu sử dụng WSC.DataAccess trong dự án của bạn.

## Bước 1: Cài đặt Package

```bash
dotnet add package WSC.DataAccess
```

## Bước 2: Tạo Database

Tạo database và bảng mẫu (SQL Server):

```sql
CREATE DATABASE MyApplicationDB;
GO

USE MyApplicationDB;
GO

CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL
);

-- Insert sample data
INSERT INTO Users (Username, FullName, Email, PasswordHash)
VALUES
    ('admin', 'Administrator', 'admin@example.com', 'hashed_password_123'),
    ('john.doe', 'John Doe', 'john.doe@example.com', 'hashed_password_123'),
    ('jane.smith', 'Jane Smith', 'jane.smith@example.com', 'hashed_password_123');
```

## Bước 3: Tạo cấu trúc thư mục

Tạo thư mục cho SQL Map files:

```
YourProject/
├── SqlMaps/              # Thư mục chứa SQL Map XML files
│   ├── DAO000.xml       # System queries
│   └── DAO001.xml       # User queries
├── Models/              # Model classes
│   ├── User.cs
│   └── Provider.cs      # DAO names constants
├── Services/            # Business logic services
│   └── UserService.cs
├── appsettings.json
└── Program.cs
```

## Bước 4: Tạo Model Class

Tạo file `Models/User.cs`:

```csharp
namespace YourProject.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

## Bước 5: Tạo Provider Class (Optional but Recommended)

Tạo file `Models/Provider.cs`:

```csharp
namespace YourProject.Models;

public static class Provider
{
    // DAO Names constants
    public const string DAO000 = "DAO000"; // System queries
    public const string DAO001 = "DAO001"; // User management
    public const string DAO002 = "DAO002"; // Product management
    // ... thêm các DAO khác

    /// <summary>
    /// Get all DAO names
    /// </summary>
    public static string[] GetAllDaoNames()
    {
        return new[] { DAO000, DAO001, DAO002 };
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
    /// Get description for DAO
    /// </summary>
    public static string GetDescription(string daoName)
    {
        var descriptions = new Dictionary<string, string>
        {
            { DAO000, "System queries" },
            { DAO001, "User management" },
            { DAO002, "Product management" }
        };

        return descriptions.TryGetValue(daoName, out var desc)
            ? desc
            : "Unknown DAO";
    }
}
```

## Bước 6: Tạo SQL Map Files

### File 1: SqlMaps/DAO000.xml (System queries)

```xml
<?xml version="1.0" encoding="utf-8" ?>
<sqlMap namespace="System">
  <!-- Test connection -->
  <select id="System.TestConnection" resultClass="int">
    <![CDATA[
      SELECT 1
    ]]>
  </select>

  <!-- Get database version -->
  <select id="System.GetDatabaseVersion" resultClass="string">
    <![CDATA[
      SELECT @@VERSION
    ]]>
  </select>

  <!-- Get current database name -->
  <select id="System.GetCurrentDatabase" resultClass="string">
    <![CDATA[
      SELECT DB_NAME()
    ]]>
  </select>

  <!-- Get current datetime -->
  <select id="System.GetCurrentDateTime" resultClass="DateTime">
    <![CDATA[
      SELECT GETDATE()
    ]]>
  </select>
</sqlMap>
```

### File 2: SqlMaps/DAO001.xml (User management)

```xml
<?xml version="1.0" encoding="utf-8" ?>
<sqlMap namespace="User">
  <!-- Get all users -->
  <select id="User.GetAllUsers" resultClass="User">
    <![CDATA[
      SELECT Id, Username, FullName, Email, PasswordHash, IsActive, CreatedAt, UpdatedAt
      FROM Users
      WHERE IsActive = 1
      ORDER BY FullName
    ]]>
  </select>

  <!-- Get user by ID -->
  <select id="User.GetUserById" resultClass="User">
    <![CDATA[
      SELECT Id, Username, FullName, Email, PasswordHash, IsActive, CreatedAt, UpdatedAt
      FROM Users
      WHERE Id = @Id
    ]]>
  </select>

  <!-- Get user by username -->
  <select id="User.GetUserByUsername" resultClass="User">
    <![CDATA[
      SELECT Id, Username, FullName, Email, PasswordHash, IsActive, CreatedAt, UpdatedAt
      FROM Users
      WHERE Username = @Username
    ]]>
  </select>

  <!-- Count users -->
  <select id="User.CountUsers" resultClass="int">
    <![CDATA[
      SELECT COUNT(*)
      FROM Users
      WHERE IsActive = 1
    ]]>
  </select>

  <!-- Insert new user -->
  <insert id="User.InsertUser">
    <![CDATA[
      INSERT INTO Users (Username, FullName, Email, PasswordHash, IsActive)
      VALUES (@Username, @FullName, @Email, @PasswordHash, @IsActive)
    ]]>
  </insert>

  <!-- Update user -->
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

  <!-- Delete user (soft delete) -->
  <update id="User.DeleteUser">
    <![CDATA[
      UPDATE Users
      SET IsActive = 0,
          UpdatedAt = GETDATE()
      WHERE Id = @Id
    ]]>
  </update>
</sqlMap>
```

## Bước 7: Cấu hình appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyApplicationDB;User Id=sa;Password=YourPassword;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "WSC.DataAccess": "Debug"
    }
  }
}
```

## Bước 8: Đăng ký Service trong Program.cs

### ASP.NET Core Web API / MVC

```csharp
using WSC.DataAccess.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

// Register WSC.DataAccess
builder.Services.AddWscDataAccess(
    builder.Configuration,
    configure: options =>
    {
        // Auto-discover all SQL Map files in SqlMaps directory
        options.AutoDiscoverSqlMaps("SqlMaps");
    });

// Add your services
builder.Services.AddScoped<UserService>();

var app = builder.Build();

app.MapControllers();
app.Run();
```

### Console Application

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Setup DI container
var services = new ServiceCollection();

// Add logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Register WSC.DataAccess
services.AddWscDataAccess(configuration, options =>
{
    options.AutoDiscoverSqlMaps("SqlMaps");
});

// Add your services
services.AddScoped<UserService>();

// Build service provider
var serviceProvider = services.BuildServiceProvider();

// Run your application
using (var scope = serviceProvider.CreateScope())
{
    var userService = scope.ServiceProvider.GetRequiredService<UserService>();
    await userService.DoWorkAsync();
}
```

## Bước 9: Tạo Service Class

Tạo file `Services/UserService.cs`:

```csharp
using Microsoft.Extensions.Logging;
using WSC.DataAccess.Core;
using WSC.DataAccess.Extensions;
using YourProject.Models;

namespace YourProject.Services;

public class UserService
{
    private readonly ISql _sql;
    private readonly ILogger<UserService> _logger;

    public UserService(ISql sql, ILogger<UserService> logger)
    {
        _sql = sql;
        _logger = logger;
    }

    /// <summary>
    /// Get all active users
    /// </summary>
    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        try
        {
            _logger.LogInformation("Getting all users");

            // Set DAO context
            _sql.GetDAO(Provider.DAO001);

            // Create connection and execute query
            using var connection = _sql.CreateConnection();
            var users = await connection.StatementExecuteQueryAsync<User>("User.GetAllUsers");

            _logger.LogInformation("Retrieved {Count} users", users.Count());
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            throw;
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    public async Task<User?> GetUserByIdAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Getting user with ID: {UserId}", userId);

            _sql.GetDAO(Provider.DAO001);
            using var connection = _sql.CreateConnection();

            var user = await connection.StatementExecuteSingleAsync<User>(
                "User.GetUserById",
                new { Id = userId });

            if (user != null)
            {
                _logger.LogInformation("Found user: {Username}", user.Username);
            }
            else
            {
                _logger.LogWarning("User {UserId} not found", userId);
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Create new user
    /// </summary>
    public async Task<bool> CreateUserAsync(User user)
    {
        try
        {
            _logger.LogInformation("Creating user: {Username}", user.Username);

            _sql.GetDAO(Provider.DAO001);
            using var connection = _sql.CreateConnection();

            var rowsAffected = await connection.StatementExecuteAsync(
                "User.InsertUser",
                user);

            _logger.LogInformation("User created successfully");
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user: {Username}", user.Username);
            throw;
        }
    }

    /// <summary>
    /// Update user
    /// </summary>
    public async Task<bool> UpdateUserAsync(User user)
    {
        try
        {
            _logger.LogInformation("Updating user: {UserId}", user.Id);

            _sql.GetDAO(Provider.DAO001);
            using var connection = _sql.CreateConnection();

            var rowsAffected = await connection.StatementExecuteAsync(
                "User.UpdateUser",
                user);

            _logger.LogInformation("User updated successfully");
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {UserId}", user.Id);
            throw;
        }
    }

    /// <summary>
    /// Delete user (soft delete)
    /// </summary>
    public async Task<bool> DeleteUserAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Deleting user: {UserId}", userId);

            _sql.GetDAO(Provider.DAO001);
            using var connection = _sql.CreateConnection();

            var rowsAffected = await connection.StatementExecuteAsync(
                "User.DeleteUser",
                new { Id = userId });

            _logger.LogInformation("User deleted successfully");
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Get total user count
    /// </summary>
    public async Task<int> GetUserCountAsync()
    {
        try
        {
            _sql.GetDAO(Provider.DAO001);
            using var connection = _sql.CreateConnection();

            var count = await connection.StatementExecuteScalarAsync<int>("User.CountUsers");

            _logger.LogInformation("Total users: {Count}", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting users");
            throw;
        }
    }
}
```

## Bước 10: Sử dụng trong Controller (ASP.NET Core)

```csharp
using Microsoft.AspNetCore.Mvc;
using YourProject.Models;
using YourProject.Services;

namespace YourProject.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetAll()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetById(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] User user)
    {
        var success = await _userService.CreateUserAsync(user);
        if (!success)
            return BadRequest();

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, [FromBody] User user)
    {
        user.Id = id;
        var success = await _userService.UpdateUserAsync(user);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var success = await _userService.DeleteUserAsync(id);
        if (!success)
            return NotFound();

        return NoContent();
    }
}
```

## Bước 11: Chạy thử

```bash
# Build project
dotnet build

# Run project
dotnet run
```

Nếu là Web API, test endpoints:

```bash
# Get all users
curl http://localhost:5000/api/users

# Get user by ID
curl http://localhost:5000/api/users/1

# Create user
curl -X POST http://localhost:5000/api/users \
  -H "Content-Type: application/json" \
  -d '{"username":"test.user","fullName":"Test User","email":"test@example.com","passwordHash":"hashed123","isActive":true}'
```

## Xem thêm

- [Configuration Guide](CONFIGURATION.md) - Các cách cấu hình khác nhau
- [Examples](EXAMPLES.md) - Các ví dụ từ sample apps
- [API Reference](API_REFERENCE.md) - Tài liệu API chi tiết
- [FAQ](FAQ.md) - Troubleshooting và câu hỏi thường gặp
