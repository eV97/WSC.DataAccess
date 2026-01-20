# iBatis Logging Test Guide

Hướng dẫn chạy test cases để verify logging hoạt động đúng.

## 📋 Danh Sách Test Programs

### 1. **QuickLoggingTest.cs** - Quick Verification Test

**Mục đích**: Test nhanh để verify logging cơ bản hoạt động

**Các test scenarios**:
- ✅ Connection open/close
- ✅ Transaction commit
- ✅ Successful query execution
- ✅ Failed query (SQL error)
- ✅ Statement not found

**Chạy test**:
```bash
cd samples/WSC.DataAccess.RealDB.Test
dotnet run --project WSC.DataAccess.RealDB.Test.csproj -- QuickLoggingTest
```

Hoặc compile và chạy:
```bash
dotnet build
# Change StartupObject in .csproj to QuickLoggingTest
dotnet run
```

**Expected output**:
```
✓ Logging initialized
Test Scenario: Simple Query with Transaction
1. Starting transaction...
2. Executing query...
   ✓ Got 1 row(s)
3. Committing transaction...
   ✓ Transaction committed

Test Scenario: Error Handling
1. Executing query that will fail...
   ✓ Error caught: Invalid object name 'TableThatDoesNotExist'
```

---

### 2. **LoggingTestProgram.cs** - Comprehensive Test Suite

**Mục đích**: Test toàn diện tất cả logging scenarios

**Các test cases**:

| Test Case | Description | Expected Logs |
|-----------|-------------|---------------|
| Test 1 | SQL Map Loading | `[INF] Loading SQL map`, `[DBG] Loaded statements` |
| Test 2 | Successful Query | `[DBG] Executing QueryAsync`, `[INF] QueryAsync completed` |
| Test 3 | Failed Query | `[ERR] QueryAsync failed` with exception |
| Test 4 | Statement Not Found | `[WRN] Statement not found` |
| Test 5 | Transaction Commit | `[INF] Transaction started`, `[INF] Transaction committed` |
| Test 6 | Transaction Rollback | `[INF] Transaction started`, `[WRN] Transaction rolled back` |
| Test 7 | Connection Lifecycle | `[INF] Connection opened`, `[INF] Connection closed` |
| Test 8 | Multiple Sessions | Multiple SessionIds in logs |

**Chạy test**:
```bash
cd samples/WSC.DataAccess.RealDB.Test
dotnet run --project WSC.DataAccess.RealDB.Test.csproj -- LoggingTestProgram
```

---

### 3. **ProgramWithSqlMaps.cs** - Real Database Test

**Mục đích**: Test với database thực tế và SQL maps

**Features**:
- Load SQL map files từ XML
- Test repository pattern
- Test direct SqlMapper usage
- Test transactions

**Chạy test**:
```bash
cd samples/WSC.DataAccess.RealDB.Test
dotnet run
```

---

## 📁 Log Files Location

Logs được ghi vào:
```
samples/WSC.DataAccess.RealDB.Test/log/iBatis/
├── ibatis-20260120.log          (Full logs)
└── ibatis-errors-20260120.log   (Errors only)
```

Hoặc:
```
samples/WSC.DataAccess.Sample/log/iBatis/
├── ibatis-20260120.log
└── ibatis-errors-20260120.log
```

---

## 🔍 Cách Verify Logs

### 1. Check Log Files Được Tạo

```bash
# Linux/Mac
ls -la log/iBatis/

# Windows
dir log\iBatis\
```

**Expected**: Thấy 2 files được tạo với ngày hiện tại

---

### 2. View Full Logs

```bash
# Linux/Mac
tail -f log/iBatis/ibatis-20260120.log

# Windows
Get-Content log\iBatis\ibatis-20260120.log -Wait -Tail 20
```

**Expected output examples**:

```
2026-01-20 14:30:15.123 +07:00 [DBG] WSC.DataAccess.Core.DbSession: DbSession created - SessionId: a1b2c3d4, Database: TestDB
2026-01-20 14:30:15.145 +07:00 [DBG] WSC.DataAccess.Core.DbSession: Opening connection - SessionId: a1b2c3d4
2026-01-20 14:30:15.234 +07:00 [INF] WSC.DataAccess.Core.DbSession: Connection opened - SessionId: a1b2c3d4, Database: TestDB
2026-01-20 14:30:15.456 +07:00 [DBG] WSC.DataAccess.Core.DbSession: Beginning transaction - SessionId: a1b2c3d4
2026-01-20 14:30:15.567 +07:00 [INF] WSC.DataAccess.Core.DbSession: Transaction started - SessionId: a1b2c3d4, IsolationLevel: ReadCommitted
2026-01-20 14:30:16.123 +07:00 [DBG] WSC.DataAccess.Mapping.SqlMapper: Executing QueryAsync - StatementId: Test.SimpleQuery, Type: ExpandoObject
2026-01-20 14:30:16.234 +07:00 [INF] WSC.DataAccess.Mapping.SqlMapper: QueryAsync completed - StatementId: Test.SimpleQuery, ResultCount: 1, Duration: 111ms
2026-01-20 14:30:16.345 +07:00 [DBG] WSC.DataAccess.Core.DbSession: Committing transaction - SessionId: a1b2c3d4
2026-01-20 14:30:16.456 +07:00 [INF] WSC.DataAccess.Core.DbSession: Transaction committed successfully - SessionId: a1b2c3d4
2026-01-20 14:30:16.567 +07:00 [DBG] WSC.DataAccess.Core.DbSession: Disposing DbSession - SessionId: a1b2c3d4
2026-01-20 14:30:16.678 +07:00 [DBG] WSC.DataAccess.Core.DbSession: Closing connection - SessionId: a1b2c3d4
2026-01-20 14:30:16.789 +07:00 [INF] WSC.DataAccess.Core.DbSession: Connection closed - SessionId: a1b2c3d4
```

---

### 3. View Error Logs Only

```bash
# Linux/Mac
cat log/iBatis/ibatis-errors-20260120.log

# Windows
type log\iBatis\ibatis-errors-20260120.log
```

**Expected**: Chỉ thấy WARNING và ERROR logs

```
2026-01-20 14:30:18.789 +07:00 [ERR] WSC.DataAccess.Mapping.SqlMapper: QueryAsync failed - StatementId: Test.ErrorQuery, Duration: 45ms
System.Data.SqlClient.SqlException: Invalid object name 'TableThatDoesNotExist'.
   at WSC.DataAccess.Mapping.SqlMapper.QueryAsync[T](DbSession session, String statementId, Object parameters)

2026-01-20 14:30:19.123 +07:00 [WRN] WSC.DataAccess.Mapping.SqlMapConfig: Statement not found: Does.NotExist
```

---

### 4. Search for Specific Patterns

```bash
# Tìm tất cả queries
grep "QueryAsync completed" log/iBatis/ibatis-20260120.log

# Tìm queries chậm (>1000ms)
grep -E "Duration: [1-9][0-9]{3,}ms" log/iBatis/ibatis-20260120.log

# Tìm tất cả errors
grep "ERR" log/iBatis/ibatis-20260120.log

# Tìm operations của một session cụ thể
grep "SessionId: a1b2c3d4" log/iBatis/ibatis-20260120.log

# Đếm số lượng queries
grep -c "QueryAsync completed" log/iBatis/ibatis-20260120.log
```

---

## ✅ Verification Checklist

Sau khi chạy tests, verify các điều sau:

### Log Files
- [ ] File `ibatis-YYYYMMDD.log` được tạo
- [ ] File `ibatis-errors-YYYYMMDD.log` được tạo
- [ ] File size > 0 bytes

### Log Levels
- [ ] **[DBG]** - Debug logs có trong full log
- [ ] **[INF]** - Information logs có trong full log
- [ ] **[WRN]** - Warning logs có trong cả 2 files
- [ ] **[ERR]** - Error logs có trong cả 2 files

### Content Verification
- [ ] SQL map loading logs
- [ ] Connection lifecycle logs (open/close)
- [ ] Transaction logs (begin/commit/rollback)
- [ ] Query execution logs với duration
- [ ] Error logs với exception details
- [ ] Session IDs để track operations

### Format Verification
- [ ] Timestamp với milliseconds: `2026-01-20 14:30:15.123 +07:00`
- [ ] Log level: `[DBG]`, `[INF]`, `[WRN]`, `[ERR]`
- [ ] Source context: `WSC.DataAccess.Core.DbSession`
- [ ] Structured logging parameters: `SessionId: xxxxxxxx`

---

## 🐛 Troubleshooting

### Vấn đề: Không thấy log files

**Nguyên nhân**: Serilog packages chưa được cài

**Giải pháp**:
```bash
cd samples/WSC.DataAccess.RealDB.Test
dotnet add package Serilog
dotnet add package Serilog.Extensions.Logging
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
```

---

### Vấn đề: Log files rỗng

**Nguyên nhân**: Logging chưa được configure

**Giải pháp**: Check `Program.cs` có:
```csharp
.ConfigureLogging((context, logging) =>
{
    logging.ClearProviders();
    logging.AddIBatisLogging();
})
```

---

### Vấn đề: Chỉ thấy console logs, không có file logs

**Nguyên nhân**: Serilog.Sinks.File package chưa được cài

**Giải pháp**:
```bash
dotnet add package Serilog.Sinks.File
```

---

### Vấn đề: Log directory permission denied

**Nguyên nhân**: Không có quyền tạo folder

**Giải pháp**:
```bash
# Linux/Mac
mkdir -p log/iBatis
chmod 755 log/iBatis

# Windows - Run as Administrator
```

---

## 📊 Expected Test Results Summary

| Component | Test Count | Expected Logs |
|-----------|-----------|---------------|
| SqlMapConfig | 2 | Load XML, Register statements |
| SqlMapper | 5 | Query/Execute/Scalar operations |
| DbSession | 8 | Open, Close, Transaction lifecycle |
| Repository | 3 | Repository operations |
| **Total** | **18** | **~50-100 log entries** |

---

## 🎯 Next Steps

Sau khi verify logging hoạt động:

1. **Production Setup**:
   - Set log level to `Information` (not Debug)
   - Configure log rotation
   - Set up monitoring/alerting

2. **Integration**:
   - Integrate với Seq/ELK/Splunk
   - Set up log aggregation
   - Configure dashboards

3. **Performance Monitoring**:
   - Track query durations
   - Monitor slow queries (>1000ms)
   - Analyze error patterns

---

## 📝 Log Analysis Examples

### Find slow queries:
```bash
grep "Duration:" log/iBatis/ibatis-20260120.log | \
  awk '{print $NF}' | \
  sed 's/ms//' | \
  sort -n | \
  tail -10
```

### Count operations per session:
```bash
grep "SessionId:" log/iBatis/ibatis-20260120.log | \
  awk '{print $7}' | \
  sort | \
  uniq -c
```

### Analyze error types:
```bash
grep "ERR" log/iBatis/ibatis-errors-20260120.log | \
  grep -oP "System\.\w+\.\w+Exception" | \
  sort | \
  uniq -c
```

---

✅ **Logging test guide complete!**

Run the tests, verify the logs, và enjoy comprehensive iBatis logging! 🎉
