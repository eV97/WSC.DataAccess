# WSC.DataAccess - Real Database Test

Test application để kết nối và truy vấn database thực tế LP_ApplicationSystem.

## Database Information

- **Server**: FHC-VUONGLH3\SQLEXPRESS02
- **Database**: LP_ApplicationSystem
- **Username**: admin
- **Password**: admin

## Cách chạy

### 1. Từ command line:

```bash
cd samples/WSC.DataAccess.RealDB.Test
dotnet run
```

### 2. Từ Visual Studio:

1. Mở solution WSC.DataAccess.sln
2. Set WSC.DataAccess.RealDB.Test là StartUp Project
3. Press F5 hoặc Ctrl+F5

## Các test được thực hiện

1. **Connection Test**: Kiểm tra kết nối đến database
2. **Database Info**: Lấy thông tin về database và SQL Server
3. **List Tables**: Liệt kê tất cả các tables trong database
4. **Sample Data**: Query dữ liệu mẫu từ table đầu tiên
5. **Table Search**: Tìm các tables theo pattern phổ biến
6. **Custom Query**: Examples về cách viết custom queries

## Cấu hình

Nếu cần thay đổi connection string, edit file `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=YOUR_DB;User Id=YOUR_USER;Password=YOUR_PASS;TrustServerCertificate=True;"
  }
}
```

## Troubleshooting

### Lỗi connection

1. Kiểm tra SQL Server đang chạy:
   - Mở Services (services.msc)
   - Tìm SQL Server (SQLEXPRESS02)
   - Đảm bảo service đang Running

2. Kiểm tra SQL Server Configuration Manager:
   - SQL Server Network Configuration
   - Protocols for SQLEXPRESS02
   - Enable TCP/IP và Named Pipes

3. Kiểm tra authentication:
   - SQL Server phải enable SQL Server Authentication
   - User 'admin' phải có quyền truy cập database

### Lỗi timeout

Tăng Connect Timeout trong connection string:

```json
"DefaultConnection": "Server=...;Connect Timeout=60;..."
```

## Ví dụ sử dụng

### Query một table cụ thể

```csharp
using (var session = sessionFactory.OpenSession())
{
    var sql = "SELECT * FROM Users WHERE IsActive = 1";
    var users = await session.Connection.QueryAsync<User>(sql);

    foreach (var user in users)
    {
        Console.WriteLine($"{user.Username} - {user.Email}");
    }
}
```

### Insert data

```csharp
using (var session = sessionFactory.OpenSession())
{
    session.BeginTransaction();

    try
    {
        var sql = @"
            INSERT INTO Users (Username, Email, CreatedDate)
            VALUES (@Username, @Email, @CreatedDate)";

        await session.Connection.ExecuteAsync(sql, new
        {
            Username = "testuser",
            Email = "test@example.com",
            CreatedDate = DateTime.Now
        }, session.Transaction);

        session.Commit();
    }
    catch
    {
        session.Rollback();
        throw;
    }
}
```

### Update data

```csharp
using (var session = sessionFactory.OpenSession())
{
    var sql = @"
        UPDATE Users
        SET Email = @Email, UpdatedDate = @UpdatedDate
        WHERE Id = @Id";

    var rowsAffected = await session.Connection.ExecuteAsync(sql, new
    {
        Id = 1,
        Email = "newemail@example.com",
        UpdatedDate = DateTime.Now
    });

    Console.WriteLine($"Updated {rowsAffected} rows");
}
```

## Output Example

```
╔══════════════════════════════════════════════════════════════════╗
║  WSC.DataAccess - Real Database Connection Test                 ║
║  Database: LP_ApplicationSystem                                  ║
╚══════════════════════════════════════════════════════════════════╝

📋 Connection Info:
   Server: FHC-VUONGLH3\SQLEXPRESS02
   Database: LP_ApplicationSystem
   User: admin

🔌 TEST 1: Testing Connection...
   Connecting to database...
   ✅ Connection successful!

📊 TEST 2: Getting Database Information...
   Current Database: LP_ApplicationSystem
   SQL Server Version: Microsoft SQL Server 2019 (RTM) - 15.0.2000.5

📁 TEST 3: Listing All Tables in Database...
   Found 25 tables:

     1. [dbo].[Users]
        └─ Id (int) NOT NULL
        └─ Username (nvarchar) NOT NULL
        └─ Email (nvarchar) NOT NULL
        └─ CreatedDate (datetime2) NOT NULL
        └─ IsActive (bit) NOT NULL

     2. [dbo].[Products]
     ...

✅ ALL TESTS PASSED SUCCESSFULLY!
```

## Next Steps

Sau khi test thành công, bạn có thể:

1. Tạo models cho các tables trong database
2. Tạo repositories cho các entities
3. Integrate vào project của bạn

Xem thêm:
- [CREATE_REPOSITORY_GUIDE.md](../../docs/CREATE_REPOSITORY_GUIDE.md)
- [IBATIS_GUIDE.md](../../docs/IBATIS_GUIDE.md)
- [ADVANCED_EXAMPLES.md](../../docs/ADVANCED_EXAMPLES.md)
