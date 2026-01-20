# WSC.DataAccess - Thư viện Truy cập Dữ liệu cho .NET 8

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

Thư viện DLL mạnh mẽ để kết nối SQL Server và quản lý truy cập dữ liệu với pattern giống IBatis cho .NET 8.

## 📋 Mục lục

- [Tính năng](#-tính-năng)
- [Cài đặt](#-cài-đặt)
- [Cấu trúc Project](#-cấu-trúc-project)
- [Hướng dẫn Sử dụng](#-hướng-dẫn-sử-dụng)
- [Các Pattern được hỗ trợ](#-các-pattern-được-hỗ-trợ)
- [SQL Mapping (IBatis-style)](#-sql-mapping-ibatis-style)
- [Ví dụ Chi tiết](#-ví-dụ-chi-tiết)
- [API Reference](#-api-reference)

## 🚀 Tính năng

- ✅ **Hỗ trợ .NET 8** - Tương thích với framework mới nhất
- ✅ **IBatis-style SQL Mapping** - Quản lý SQL bằng XML giống IBatis
- ✅ **Repository Pattern** - BaseRepository và SqlMapRepository
- ✅ **Connection Management** - Quản lý kết nối hiệu quả
- ✅ **Transaction Support** - Hỗ trợ transaction đầy đủ
- ✅ **Dependency Injection** - Tích hợp Microsoft.Extensions.DependencyInjection
- ✅ **Dapper Integration** - Sử dụng Dapper cho hiệu suất cao
- ✅ **Multiple Database Support** - Hỗ trợ nhiều connection string
- ✅ **Session Management** - DbSession pattern giống Hibernate/IBatis

## 📦 Cài đặt

### 1. Thêm Reference vào Project

```xml
<ItemGroup>
  <ProjectReference Include="path\to\WSC.DataAccess\WSC.DataAccess.csproj" />
</ItemGroup>
```

### 2. Hoặc Build DLL và Reference

```bash
cd src/WSC.DataAccess
dotnet build -c Release
```

Sau đó thêm DLL vào project của bạn:

```xml
<ItemGroup>
  <Reference Include="WSC.DataAccess">
    <HintPath>path\to\WSC.DataAccess.dll</HintPath>
  </Reference>
</ItemGroup>
```

## 📁 Cấu trúc Project

```
WSC.DataAccess/
├── src/
│   └── WSC.DataAccess/              # DLL chính
│       ├── Core/                     # Core infrastructure
│       │   ├── IDbConnectionFactory.cs
│       │   ├── SqlConnectionFactory.cs
│       │   ├── DbSession.cs
│       │   ├── IDbSessionFactory.cs
│       │   └── DbSessionFactory.cs
│       ├── Mapping/                  # IBatis-style SQL mapping
│       │   ├── SqlStatement.cs
│       │   ├── SqlMapConfig.cs
│       │   └── SqlMapper.cs
│       ├── Repository/               # Repository patterns
│       │   ├── IRepository.cs
│       │   ├── BaseRepository.cs
│       │   └── SqlMapRepository.cs
│       ├── Configuration/            # DI configuration
│       │   └── DataAccessServiceCollectionExtensions.cs
│       ├── Examples/                 # Example models & repos
│       │   ├── Models/
│       │   └── Repositories/
│       └── SqlMaps/                  # SQL XML maps
│           └── ProductMap.xml
├── samples/
│   └── WSC.DataAccess.Sample/       # Sample application
└── README.md
```

## 🎯 Hướng dẫn Sử dụng

### 1. Cấu hình appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  }
}
```

### 2. Đăng ký Services trong Program.cs

```csharp
using Microsoft.Extensions.DependencyInjection;
using WSC.DataAccess.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Lấy connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Đăng ký WSC Data Access
builder.Services.AddWscDataAccess(connectionString, options =>
{
    // Thêm connection strings có tên
    options.AddConnection("Reporting", "Server=...;Database=ReportingDb;...");

    // Thêm SQL map files (cho IBatis-style)
    options.AddSqlMapFile("SqlMaps/ProductMap.xml");
    options.AddSqlMapFile("SqlMaps/OrderMap.xml");
});

// Đăng ký repositories
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<ProductRepository>();

var app = builder.Build();
```

### 3. Tạo Model

```csharp
using System.ComponentModel.DataAnnotations.Schema;

[Table("Users")]
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsActive { get; set; }
}
```

## 🔧 Các Pattern được hỗ trợ

### Pattern 1: BaseRepository (Dapper-based)

Sử dụng khi bạn muốn code SQL trực tiếp trong C#.

```csharp
using WSC.DataAccess.Core;
using WSC.DataAccess.Repository;
using Dapper;

public class UserRepository : BaseRepository<User>
{
    public UserRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory, "Users", "Id")
    {
    }

    public override async Task<int> InsertAsync(User entity)
    {
        var sql = @"
            INSERT INTO Users (Username, Email, FullName, CreatedDate, IsActive)
            VALUES (@Username, @Email, @FullName, @CreatedDate, @IsActive);
            SELECT CAST(SCOPE_IDENTITY() as int)";

        using var session = SessionFactory.OpenSession();
        return await session.Connection.ExecuteScalarAsync<int>(sql, entity);
    }

    public override async Task<int> UpdateAsync(User entity)
    {
        var sql = @"
            UPDATE Users
            SET Username = @Username,
                Email = @Email,
                FullName = @FullName,
                IsActive = @IsActive
            WHERE Id = @Id";

        using var session = SessionFactory.OpenSession();
        return await session.Connection.ExecuteAsync(sql, entity);
    }

    // Custom methods
    public async Task<User?> GetByUsernameAsync(string username)
    {
        var sql = "SELECT * FROM Users WHERE Username = @Username";
        using var session = SessionFactory.OpenSession();
        return await session.Connection.QueryFirstOrDefaultAsync<User>(
            sql, new { Username = username });
    }
}
```

### Pattern 2: SqlMapRepository (IBatis-style)

Sử dụng khi bạn muốn quản lý SQL bằng XML files giống IBatis.

#### Bước 1: Tạo SQL Map XML

```xml
<?xml version="1.0" encoding="utf-8" ?>
<sqlMap namespace="Product">

  <!-- Get all products -->
  <select id="Product.GetAll" resultType="MyApp.Models.Product">
    SELECT * FROM Products WHERE IsActive = 1 ORDER BY ProductName
  </select>

  <!-- Get by ID -->
  <select id="Product.GetById" resultType="MyApp.Models.Product">
    SELECT * FROM Products WHERE Id = @Id
  </select>

  <!-- Insert product -->
  <insert id="Product.Insert">
    INSERT INTO Products (ProductCode, ProductName, Price, StockQuantity, CreatedDate, IsActive)
    VALUES (@ProductCode, @ProductName, @Price, @StockQuantity, @CreatedDate, @IsActive)
  </insert>

  <!-- Update product -->
  <update id="Product.Update">
    UPDATE Products
    SET ProductName = @ProductName,
        Price = @Price,
        StockQuantity = @StockQuantity,
        UpdatedDate = @UpdatedDate
    WHERE Id = @Id
  </update>

</sqlMap>
```

#### Bước 2: Tạo Repository

```csharp
using WSC.DataAccess.Core;
using WSC.DataAccess.Mapping;
using WSC.DataAccess.Repository;

public class ProductRepository : SqlMapRepository<Product>
{
    public ProductRepository(IDbSessionFactory sessionFactory, SqlMapper sqlMapper)
        : base(sessionFactory, sqlMapper)
    {
    }

    public async Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        return await QueryListAsync("Product.GetAll");
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await QuerySingleAsync("Product.GetById", new { Id = id });
    }

    public async Task<int> InsertAsync(Product product)
    {
        return await ExecuteAsync("Product.Insert", product);
    }

    public async Task<int> UpdateAsync(Product product)
    {
        return await ExecuteAsync("Product.Update", product);
    }
}
```

## 📝 SQL Mapping (IBatis-style)

### Các Element được hỗ trợ

| Element | Mô tả | Attributes |
|---------|-------|------------|
| `<select>` | Query SELECT | id, resultType, timeout |
| `<insert>` | INSERT statement | id, parameterType, timeout |
| `<update>` | UPDATE statement | id, parameterType, timeout |
| `<delete>` | DELETE statement | id, parameterType, timeout |
| `<procedure>` | Stored procedure | id, resultType, timeout |

### Ví dụ Stored Procedure

```xml
<procedure id="Product.GetTopSellers" resultType="MyApp.Models.Product">
  usp_GetTopSellingProducts
</procedure>
```

```csharp
public async Task<IEnumerable<Product>> GetTopSellersAsync(int top)
{
    using var session = SessionFactory.OpenSession();
    return await SqlMapper.ExecuteProcedureAsync<Product>(
        session, "Product.GetTopSellers", new { Top = top });
}
```

## 💡 Ví dụ Chi tiết

### 1. Sử dụng Transactions

```csharp
public class OrderService
{
    private readonly IDbSessionFactory _sessionFactory;

    public OrderService(IDbSessionFactory sessionFactory)
    {
        _sessionFactory = sessionFactory;
    }

    public async Task<int> CreateOrderWithItemsAsync(Order order, List<OrderItem> items)
    {
        using var session = _sessionFactory.OpenSession();
        session.BeginTransaction();

        try
        {
            // Insert order
            var orderId = await session.Connection.ExecuteScalarAsync<int>(
                "INSERT INTO Orders (...) VALUES (...); SELECT SCOPE_IDENTITY()",
                order,
                session.Transaction);

            // Insert order items
            foreach (var item in items)
            {
                item.OrderId = orderId;
                await session.Connection.ExecuteAsync(
                    "INSERT INTO OrderItems (...) VALUES (...)",
                    item,
                    session.Transaction);
            }

            session.Commit();
            return orderId;
        }
        catch
        {
            session.Rollback();
            throw;
        }
    }
}
```

### 2. Sử dụng Multiple Databases

```csharp
// Configuration
builder.Services.AddWscDataAccess(mainConnectionString, options =>
{
    options.AddConnection("Analytics", analyticsConnectionString);
    options.AddConnection("Archive", archiveConnectionString);
});

// Usage
public class ReportRepository
{
    private readonly IDbSessionFactory _sessionFactory;

    public async Task<IEnumerable<Report>> GetAnalyticsReportsAsync()
    {
        // Sử dụng connection "Analytics"
        using var session = _sessionFactory.OpenSession("Analytics");
        return await session.Connection.QueryAsync<Report>(
            "SELECT * FROM Reports WHERE ReportDate >= @Date",
            new { Date = DateTime.Now.AddMonths(-1) });
    }
}
```

### 3. Custom Query với BaseRepository

```csharp
public class UserRepository : BaseRepository<User>
{
    // ... constructor ...

    public async Task<IEnumerable<User>> SearchUsersAsync(string keyword)
    {
        var sql = @"
            SELECT * FROM Users
            WHERE (Username LIKE @Keyword OR Email LIKE @Keyword)
              AND IsActive = 1
            ORDER BY Username";

        return await QueryAsync(sql, new { Keyword = $"%{keyword}%" });
    }

    public async Task<int> DeactivateInactiveUsersAsync(int daysInactive)
    {
        var sql = @"
            UPDATE Users
            SET IsActive = 0
            WHERE LastLoginDate < @CutoffDate";

        return await ExecuteAsync(sql, new
        {
            CutoffDate = DateTime.Now.AddDays(-daysInactive)
        });
    }
}
```

### 4. Sử dụng trong Console Application

```csharp
using Microsoft.Extensions.DependencyInjection;
using WSC.DataAccess.Configuration;

class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        // Register data access
        services.AddWscDataAccess("Server=...;Database=...;", options =>
        {
            options.AddSqlMapFile("SqlMaps/UserMap.xml");
        });

        services.AddScoped<UserRepository>();

        var serviceProvider = services.BuildServiceProvider();

        // Use repository
        using (var scope = serviceProvider.CreateScope())
        {
            var userRepo = scope.ServiceProvider.GetRequiredService<UserRepository>();
            var users = await userRepo.GetAllAsync();

            foreach (var user in users)
            {
                Console.WriteLine($"{user.Username} - {user.Email}");
            }
        }
    }
}
```

### 5. Sử dụng trong ASP.NET Core Web API

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserRepository _userRepository;

    public UsersController(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetAll()
    {
        var users = await _userRepository.GetAllAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetById(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create(User user)
    {
        user.CreatedDate = DateTime.Now;
        var userId = await _userRepository.InsertAsync(user);
        return CreatedAtAction(nameof(GetById), new { id = userId }, userId);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, User user)
    {
        user.Id = id;
        var result = await _userRepository.UpdateAsync(user);
        if (result == 0)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var result = await _userRepository.DeleteAsync(id);
        if (result == 0)
            return NotFound();

        return NoContent();
    }
}
```

## 📚 API Reference

### Core Interfaces

#### IDbConnectionFactory
```csharp
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
    IDbConnection CreateConnection(string connectionString);
    string ConnectionString { get; }
}
```

#### IDbSessionFactory
```csharp
public interface IDbSessionFactory
{
    DbSession OpenSession();
    DbSession OpenSession(string connectionName);
}
```

#### DbSession
```csharp
public class DbSession : IDisposable
{
    public IDbConnection Connection { get; }
    public IDbTransaction? Transaction { get; }

    public void BeginTransaction();
    public void BeginTransaction(IsolationLevel isolationLevel);
    public void Commit();
    public void Rollback();
    public IDbCommand CreateCommand();
}
```

### Repository Interfaces

#### IRepository<T>
```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(object id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<int> InsertAsync(T entity);
    Task<int> UpdateAsync(T entity);
    Task<int> DeleteAsync(object id);
}
```

### SqlMapper Methods

```csharp
public class SqlMapper
{
    Task<IEnumerable<T>> QueryAsync<T>(DbSession session, string statementId, object? parameters);
    Task<T?> QuerySingleAsync<T>(DbSession session, string statementId, object? parameters);
    Task<int> ExecuteAsync(DbSession session, string statementId, object? parameters);
    Task<IEnumerable<T>> ExecuteProcedureAsync<T>(DbSession session, string statementId, object? parameters);
    Task<T?> ExecuteScalarAsync<T>(DbSession session, string statementId, object? parameters);
}
```

## 🔍 Best Practices

1. **Luôn sử dụng `using` với DbSession** để đảm bảo connection được đóng
2. **Sử dụng transactions** cho các operations có nhiều bước
3. **Parameterize queries** để tránh SQL injection
4. **Sử dụng async/await** cho tất cả database operations
5. **Tách SQL ra XML files** cho complex queries (IBatis-style)
6. **Implement custom repositories** thay vì expose generic repository
7. **Sử dụng connection pooling** (được tự động handle bởi SqlConnection)

## 📄 License

MIT License - xem file LICENSE để biết thêm chi tiết

## 👥 Đóng góp

Contributions, issues và feature requests đều được chào đón!

## 📧 Liên hệ

WSC Development Team - [email@example.com](mailto:email@example.com)

---

**Chúc bạn code vui vẻ! 🚀**
