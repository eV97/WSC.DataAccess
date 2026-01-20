# Hướng dẫn Sử dụng SqlMaps (IBatis-style)

Demo chi tiết về cách sử dụng SQL Mapping (IBatis-style) với database thực tế LP_ApplicationSystem.

## 📋 Tổng quan

Project này bao gồm 2 programs:

1. **Program.cs** - Test kết nối và explore database
2. **ProgramWithSqlMaps.cs** - Demo sử dụng SqlMaps (IBatis-style)

## 🚀 Cách chạy

### Bước 1: Chạy Program.cs để explore database

```bash
cd samples/WSC.DataAccess.RealDB.Test
dotnet run
```

Điều này sẽ:
- List tất cả tables trong database
- Hiển thị column names và data types
- Giúp bạn hiểu cấu trúc database

### Bước 2: Customize SQL Maps cho database của bạn

Dựa trên output của bước 1, update các files:

#### 2.1. Update Model (Models/Application.cs)

```csharp
public class Application
{
    // Adjust properties để match với columns trong database
    public int Id { get; set; }
    public string ApplicationName { get; set; }
    // ... thêm các properties khác
}
```

#### 2.2. Update SQL Map (SqlMaps/ApplicationMap.xml)

```xml
<select id="Application.GetAll">
  SELECT
    Id,
    ApplicationName,  <!-- Đổi column names cho đúng -->
    Description,
    Version
  FROM YourActualTableName  <!-- Đổi table name cho đúng -->
  WHERE IsActive = 1
</select>
```

### Bước 3: Chạy Demo SqlMaps

Có 2 cách:

**Cách 1: Chỉnh sửa Program.cs**

Đổi entry point trong `.csproj`:

```xml
<PropertyGroup>
  <StartupObject>WSC.DataAccess.RealDB.Test.ProgramWithSqlMaps</StartupObject>
</PropertyGroup>
```

Rồi chạy:
```bash
dotnet run
```

**Cách 2: Build và chạy riêng**

```bash
dotnet build
dotnet run --project WSC.DataAccess.RealDB.Test.csproj
```

Sau đó thủ công gọi ProgramWithSqlMaps trong code.

## 📁 Cấu trúc Files

```
WSC.DataAccess.RealDB.Test/
├── Program.cs                    # Database explorer
├── ProgramWithSqlMaps.cs         # SqlMaps demo ⭐
├── appsettings.json              # Connection string
├── Models/
│   └── Application.cs            # Model class
├── Repositories/
│   └── ApplicationRepository.cs  # Repository using SqlMaps ⭐
└── SqlMaps/                      # SQL Map files ⭐
    ├── ApplicationMap.xml        # Application queries
    └── GenericMap.xml            # Generic queries
```

## 🎯 Demos Included

### Demo 1: SqlMapRepository Pattern

```csharp
var appRepo = services.GetRequiredService<ApplicationRepository>();

// Query sử dụng SQL từ XML
var apps = await appRepo.GetAllApplicationsAsync();
// → Thực thi SQL: Application.GetAll từ ApplicationMap.xml

var app = await appRepo.GetByIdAsync(1);
// → Thực thi SQL: Application.GetById từ ApplicationMap.xml
```

### Demo 2: Direct SqlMapper Usage

```csharp
var sqlMapper = services.GetRequiredService<SqlMapper>();

using var session = sessionFactory.OpenSession();
var results = await sqlMapper.QueryAsync<Application>(
    session,
    "Application.GetAll",  // Statement ID từ XML
    null);
```

### Demo 3: Transaction với SqlMaps

```csharp
await appRepo.ExecuteInTransactionAsync(async (session) =>
{
    // Insert
    await sqlMapper.ExecuteAsync(session, "Application.Insert", newApp);

    // Update
    await sqlMapper.ExecuteAsync(session, "Application.Update", app);

    return true;
});
```

### Demo 4: So sánh XML vs Code

**Cách 1: SQL trong XML (IBatis-style) ⭐**

File: `SqlMaps/ApplicationMap.xml`
```xml
<select id="Application.GetAll">
  SELECT * FROM Applications WHERE IsActive = 1
</select>
```

Code:
```csharp
var apps = await appRepo.GetAllApplicationsAsync();
```

**Ưu điểm:**
- ✅ SQL tập trung, dễ maintain
- ✅ DBA có thể review/optimize SQL
- ✅ Tái sử dụng queries
- ✅ Phù hợp complex queries

**Cách 2: SQL trong Code (Dapper)**

```csharp
var sql = "SELECT * FROM Applications WHERE IsActive = 1";
var apps = await connection.QueryAsync<Application>(sql);
```

**Ưu điểm:**
- ✅ Đơn giản, nhanh
- ✅ Phù hợp simple queries

## 📝 Customize cho Database của bạn

### Bước 1: Xác định Table cần làm việc

Chạy `Program.cs` để xem tables. Giả sử bạn thấy table `tbl_Users`:

```
Tables found:
  1. [dbo].[tbl_Users]
      └─ UserID (int) NOT NULL
      └─ Username (nvarchar) NOT NULL
      └─ Email (nvarchar) NOT NULL
      └─ IsActive (bit) NOT NULL
```

### Bước 2: Tạo Model

File: `Models/User.cs`

```csharp
public class User
{
    public int UserID { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
```

### Bước 3: Tạo SQL Map

File: `SqlMaps/UserMap.xml`

```xml
<?xml version="1.0" encoding="utf-8" ?>
<sqlMap namespace="User">

  <select id="User.GetAll" resultType="YourNamespace.Models.User">
    SELECT UserID, Username, Email, IsActive
    FROM tbl_Users
    WHERE IsActive = 1
    ORDER BY Username
  </select>

  <select id="User.GetById" resultType="YourNamespace.Models.User">
    SELECT UserID, Username, Email, IsActive
    FROM tbl_Users
    WHERE UserID = @Id
  </select>

  <insert id="User.Insert">
    INSERT INTO tbl_Users (Username, Email, IsActive)
    VALUES (@Username, @Email, @IsActive)
  </insert>

</sqlMap>
```

### Bước 4: Tạo Repository

File: `Repositories/UserRepository.cs`

```csharp
public class UserRepository : SqlMapRepository<User>
{
    public UserRepository(IDbSessionFactory sessionFactory, SqlMapper sqlMapper)
        : base(sessionFactory, sqlMapper)
    {
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await QueryListAsync("User.GetAll");
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await QuerySingleAsync("User.GetById", new { Id = id });
    }

    public async Task<int> InsertAsync(User user)
    {
        return await ExecuteAsync("User.Insert", user);
    }
}
```

### Bước 5: Đăng ký trong DI

```csharp
services.AddWscDataAccess(connectionString, options =>
{
    options.AddSqlMapFile("SqlMaps/UserMap.xml");
});

services.AddScoped<UserRepository>();
```

### Bước 6: Sử dụng

```csharp
var userRepo = services.GetRequiredService<UserRepository>();

// Get all users
var users = await userRepo.GetAllAsync();

// Get user by ID
var user = await userRepo.GetByIdAsync(1);

// Insert new user
var newUser = new User
{
    Username = "john.doe",
    Email = "john@example.com",
    IsActive = true
};
await userRepo.InsertAsync(newUser);
```

## 🔧 Troubleshooting

### Lỗi: "SQL statement 'Application.GetAll' not found"

**Nguyên nhân:** SQL Map file không được load.

**Giải pháp:**
1. Check file path đúng: `SqlMaps/ApplicationMap.xml`
2. Verify trong `.csproj` có:
   ```xml
   <None Update="SqlMaps\**\*.xml">
     <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
   </None>
   ```
3. Build lại project: `dotnet build`

### Lỗi: "Invalid object name 'Applications'"

**Nguyên nhân:** Table name trong XML không đúng.

**Giải pháp:**
1. Chạy `Program.cs` để xem đúng table name
2. Update trong XML: `FROM YourActualTableName`

### Lỗi: "Invalid column name"

**Nguyên nhân:** Column name trong SQL không match với database.

**Giải pháp:**
1. Check column names từ output của `Program.cs`
2. Update SELECT statement trong XML

### Lỗi: "Could not find type 'WSC.DataAccess.RealDB.Test.Models.Application'"

**Nguyên nhân:** resultType trong XML không đúng.

**Giải pháp:**
1. Verify namespace của Model class
2. Update `resultType` trong XML với full namespace

## 📚 Advanced Usage

### Dynamic Parameters

```xml
<select id="User.SearchByName">
  SELECT * FROM Users
  WHERE Username LIKE @SearchTerm
    AND (@IsActive IS NULL OR IsActive = @IsActive)
</select>
```

```csharp
var users = await QueryListAsync("User.SearchByName", new
{
    SearchTerm = "%john%",
    IsActive = (bool?)true
});
```

### Multiple Results

```xml
<select id="User.GetUserWithOrders">
  SELECT u.*, o.*
  FROM Users u
  LEFT JOIN Orders o ON u.UserID = o.UserID
  WHERE u.UserID = @UserId
</select>
```

### Stored Procedures

```xml
<procedure id="User.GetActiveUsers">
  usp_GetActiveUsers
</procedure>
```

```csharp
using var session = sessionFactory.OpenSession();
var users = await sqlMapper.ExecuteProcedureAsync<User>(
    session,
    "User.GetActiveUsers",
    new { MinDate = DateTime.Now.AddYears(-1) });
```

## 🎓 Best Practices

1. **Naming Convention**
   - Statement ID: `Entity.Action` (e.g., `User.GetAll`, `Product.Insert`)
   - File name: `EntityMap.xml` (e.g., `UserMap.xml`)

2. **SQL Formatting**
   - Indent cho dễ đọc
   - Comment cho complex queries
   - List columns rõ ràng (avoid `SELECT *` trong production)

3. **Parameters**
   - Luôn dùng parameters (`@ParamName`)
   - Không concatenate strings vào SQL

4. **Organization**
   - Một file XML cho một entity
   - Group related queries together

5. **Testing**
   - Test từng statement riêng biệt
   - Verify parameters matching
   - Check error handling

## 📖 Further Reading

- [IBATIS_GUIDE.md](../../docs/IBATIS_GUIDE.md) - Chi tiết về IBatis pattern
- [CREATE_REPOSITORY_GUIDE.md](../../docs/CREATE_REPOSITORY_GUIDE.md) - Tạo repositories
- [ADVANCED_EXAMPLES.md](../../docs/ADVANCED_EXAMPLES.md) - Advanced scenarios

---

**Chúc bạn sử dụng SqlMaps thành công!** 🚀
