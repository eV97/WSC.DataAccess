# iBatis Logging Integration Guide

Hướng dẫn sử dụng logging trong WSC.DataAccess để ghi nhận và theo dõi các hoạt động iBatis.

## Tổng quan

Logging đã được tích hợp vào tất cả các thành phần chính của iBatis:
- ✅ **SqlMapConfig**: Ghi log khi load SQL map files
- ✅ **SqlMapper**: Ghi log khi thực thi SQL statements
- ✅ **DbSession**: Ghi log connection và transaction management
- ✅ **SqlMapRepository**: Ghi log các repository operations

## Yêu Cầu (Requirements)

### NuGet Packages

**QUAN TRỌNG**: Để sử dụng iBatis logging, project của bạn cần có các Serilog packages sau:

```xml
<PackageReference Include="Serilog" Version="3.1.1" />
<PackageReference Include="Serilog.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
```

**Lưu ý**: Mặc dù WSC.DataAccess library đã có Serilog, nhưng **application project** (console app, web app) cũng cần cài các packages này để sử dụng `logging.AddIBatisLogging()`.

### Lý do tại sao cần packages ở application project?

- WSC.DataAccess chứa logging code và Serilog dependency
- Nhưng extension method `AddIBatisLogging()` cần Serilog packages ở application project để hoạt động
- Nếu thiếu packages, bạn sẽ không thấy logs được ghi ra

## Cấu hình Logging

### 1. Thư mục Log Files

Mặc định, log files được lưu tại: `log/iBatis/`

Có 2 loại log files:
- `ibatis-YYYYMMDD.log` - Tất cả logs (Information, Debug, Warning, Error)
- `ibatis-errors-YYYYMMDD.log` - Chỉ logs Warning và Error

### 2. Cấu hình trong Program.cs

```csharp
using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging((context, logging) =>
    {
        // Configure iBatis logging
        logging.ClearProviders();
        logging.AddIBatisLogging();

        // Hoặc tùy chỉnh log directory và level
        logging.AddIBatisLogging(
            logDirectory: "custom/log/path",
            minimumLevel: Serilog.Events.LogEventLevel.Debug
        );
    })
    .ConfigureServices((context, services) =>
    {
        services.AddWscDataAccess(connectionString, options =>
        {
            options.AddSqlMapFile("SqlMaps/YourMap.xml");
        });
    })
    .Build();
```

## Log Levels

### Information
- SQL map file loaded thành công
- SQL statement execution thành công (với duration và result count)
- Connection opened/closed
- Transaction committed

### Debug
- Các bước chi tiết trong quá trình load SQL maps
- SQL statement execution bắt đầu
- Repository method calls
- Session lifecycle events

### Warning
- Statement không tìm thấy
- Statement bị overwrite
- Transaction rollback
- Transaction chưa commit trước khi dispose

### Error
- SQL execution failures
- Transaction commit/rollback failures
- File not found
- Invalid operations

## Ví dụ Log Output

### SQL Map Loading
```
[12:34:56 INF] WSC.DataAccess.Mapping.SqlMapConfig: Loading SQL map file: SqlMaps/ApplicationMap.xml
[12:34:56 DBG] WSC.DataAccess.Mapping.SqlMapConfig: Loaded SELECT statement: Application.GetAll
[12:34:56 DBG] WSC.DataAccess.Mapping.SqlMapConfig: Loaded SELECT statement: Application.GetById
[12:34:56 INF] WSC.DataAccess.Mapping.SqlMapConfig: Successfully loaded SQL map file: SqlMaps/ApplicationMap.xml. Total statements: 5 (SELECT: 3, INSERT: 1, UPDATE: 1, DELETE: 0, PROCEDURE: 0)
```

### SQL Execution
```
[12:35:10 DBG] WSC.DataAccess.Mapping.SqlMapper: Executing QueryAsync - StatementId: Application.GetAll, Type: Application
[12:35:10 INF] WSC.DataAccess.Mapping.SqlMapper: QueryAsync completed - StatementId: Application.GetAll, ResultCount: 15, Duration: 45ms
```

### Connection & Transaction
```
[12:35:15 DBG] WSC.DataAccess.Core.DbSession: DbSession created - SessionId: a1b2c3d4, Database: LP_ApplicationSystem
[12:35:15 INF] WSC.DataAccess.Core.DbSession: Connection opened - SessionId: a1b2c3d4, Database: LP_ApplicationSystem
[12:35:16 DBG] WSC.DataAccess.Core.DbSession: Beginning transaction - SessionId: a1b2c3d4
[12:35:16 INF] WSC.DataAccess.Core.DbSession: Transaction started - SessionId: a1b2c3d4, IsolationLevel: ReadCommitted
[12:35:17 DBG] WSC.DataAccess.Core.DbSession: Committing transaction - SessionId: a1b2c3d4
[12:35:17 INF] WSC.DataAccess.Core.DbSession: Transaction committed successfully - SessionId: a1b2c3d4
[12:35:18 INF] WSC.DataAccess.Core.DbSession: Connection closed - SessionId: a1b2c3d4
```

### Error Logging
```
[12:36:20 ERR] WSC.DataAccess.Mapping.SqlMapper: QueryAsync failed - StatementId: Application.GetAll, Duration: 120ms
System.Data.SqlClient.SqlException: Invalid column name 'ApplicationName'
   at WSC.DataAccess.Mapping.SqlMapper.QueryAsync[T](DbSession session, String statementId, Object parameters)
```

## Performance Monitoring

Mỗi SQL execution được log với:
- **StatementId**: ID của statement được thực thi
- **Duration**: Thời gian thực thi (ms)
- **ResultCount/RowsAffected**: Số lượng records
- **SessionId**: ID của database session

Ví dụ:
```
QueryAsync completed - StatementId: Application.GetAll, ResultCount: 15, Duration: 45ms
ExecuteAsync completed - StatementId: Application.Update, RowsAffected: 1, Duration: 23ms
```

## Troubleshooting với Logs

### 1. SQL Statement không tìm thấy
```
[WRN] Statement not found: Application.GetById
```
**Giải pháp**: Kiểm tra xem SQL map file đã được load chưa và statement ID có đúng không.

### 2. SQL execution chậm
```
[INF] QueryAsync completed - StatementId: Application.Search, ResultCount: 10000, Duration: 5432ms
```
**Giải pháp**: Review SQL query, thêm indexes, hoặc tối ưu hóa query.

### 3. Transaction không được commit
```
[WRN] Transaction not committed or rolled back before dispose - SessionId: a1b2c3d4. Rolling back automatically.
```
**Giải pháp**: Đảm bảo luôn gọi `session.Commit()` hoặc `session.Rollback()` trong try-catch.

### 4. Connection leaks
Nếu thấy nhiều connection opened mà không có connection closed:
```
[INF] Connection opened - SessionId: a1b2c3d4
[INF] Connection opened - SessionId: e5f6g7h8
```
**Giải pháp**: Đảm bảo sử dụng `using` statement với DbSession.

## Log File Management

### Retention Policy
- **ibatis-YYYYMMDD.log**: Giữ 30 ngày
- **ibatis-errors-YYYYMMDD.log**: Giữ 90 ngày

### Log File Location
Logs được lưu tại: `{ApplicationDirectory}/log/iBatis/`

### Tùy chỉnh Log Directory
```csharp
logging.AddIBatisLogging(
    logDirectory: "/var/log/myapp/ibatis",
    minimumLevel: Serilog.Events.LogEventLevel.Information
);
```

## Best Practices

1. **Production Environment**:
   - Set log level to `Information` hoặc `Warning`
   - Monitor log file size
   - Set up log rotation

2. **Development Environment**:
   - Set log level to `Debug` để xem chi tiết
   - Review logs thường xuyên để tìm performance issues

3. **Testing**:
   - Enable `Debug` logging để troubleshoot
   - Review execution times
   - Verify transaction handling

## Integration với Monitoring Tools

Logs có thể được tích hợp với:
- **Seq**: Structured logging server
- **Elasticsearch**: Log aggregation
- **Application Insights**: Azure monitoring
- **Splunk**: Enterprise logging platform

Ví dụ với Seq:
```csharp
logging.AddSerilog(new LoggerConfiguration()
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger());
```

## Tóm tắt

✅ Logging tự động cho tất cả iBatis operations
✅ Performance monitoring với execution times
✅ Error tracking với stack traces
✅ Transaction lifecycle tracking
✅ Connection management monitoring
✅ Configurable log levels và directories
✅ Rolling file logs với retention policies

Logging giúp bạn:
- 🔍 Debug issues nhanh hơn
- 📊 Monitor performance
- 🐛 Track errors và exceptions
- 📈 Analyze usage patterns
- 🔒 Audit database operations
