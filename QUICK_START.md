# Quick Start Guide - WSC.DataAccess

Hướng dẫn nhanh để bắt đầu với WSC.DataAccess trong 5 phút.

## Bước 1: Chuẩn bị Database

Chạy script tạo database mẫu:

```bash
# Sử dụng SQL Server Management Studio hoặc Azure Data Studio
# Mở và chạy file: database/sample-schema.sql
```

Hoặc sử dụng sqlcmd:

```bash
sqlcmd -S localhost -U sa -P YourPassword -i database/sample-schema.sql
```

## Bước 2: Thêm vào Project của bạn

### Option A: Project Reference

```xml
<ItemGroup>
  <ProjectReference Include="path\to\WSC.DataAccess\src\WSC.DataAccess\WSC.DataAccess.csproj" />
</ItemGroup>
```

### Option B: Build và Reference DLL

```bash
cd src/WSC.DataAccess
dotnet build -c Release
```

Sau đó copy `bin/Release/net8.0/WSC.DataAccess.dll` vào project của bạn.

## Bước 3: Cấu hình trong Program.cs

```csharp
using WSC.DataAccess.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký WSC Data Access
builder.Services.AddWscDataAccess(
    "Server=localhost;Database=SampleDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
);

// Đăng ký repositories của bạn
builder.Services.AddScoped<UserRepository>();

var app = builder.Build();
```

## Bước 4: Tạo Model

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

## Bước 5: Tạo Repository

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
            UPDATE Users SET Username = @Username, Email = @Email,
                   FullName = @FullName, IsActive = @IsActive
            WHERE Id = @Id";

        using var session = SessionFactory.OpenSession();
        return await session.Connection.ExecuteAsync(sql, entity);
    }
}
```

## Bước 6: Sử dụng trong Controller/Service

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
    public async Task<IActionResult> GetAll()
    {
        var users = await _userRepository.GetAllAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user == null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create(User user)
    {
        user.CreatedDate = DateTime.Now;
        var id = await _userRepository.InsertAsync(user);
        return CreatedAtAction(nameof(GetById), new { id }, user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, User user)
    {
        user.Id = id;
        await _userRepository.UpdateAsync(user);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _userRepository.DeleteAsync(id);
        return NoContent();
    }
}
```

## Chạy Sample Project

```bash
cd samples/WSC.DataAccess.Sample
dotnet run
```

## Các Pattern có sẵn

### 1. BaseRepository Pattern
✅ Viết SQL trực tiếp trong C#
✅ Đơn giản, dễ hiểu
✅ Phù hợp với queries đơn giản

### 2. SqlMapRepository Pattern (IBatis-style)
✅ Quản lý SQL bằng XML
✅ Tách biệt logic SQL khỏi code
✅ Phù hợp với complex queries

## Tiếp theo?

- Xem [README.md](README.md) để biết chi tiết về tất cả tính năng
- Xem folder `samples/` để có ví dụ hoàn chỉnh
- Xem folder `src/WSC.DataAccess/Examples/` để học cách implement

## Cần trợ giúp?

- Đọc API Reference trong README.md
- Xem source code trong folder Examples/
- Chạy sample project để test

**Chúc bạn code vui vẻ!** 🚀
