# How to Run Log Test

Quick guide để test iBatis logging.

## 🚀 Chạy Test

### Cách 1: Chạy trực tiếp (Nhanh nhất)

```bash
cd samples/WSC.DataAccess.RealDB.Test
dotnet run --project WSC.DataAccess.RealDB.Test.csproj
```

**Lưu ý**: File này sẽ chạy program mặc định trong project. Để chạy LogTest.cs, cần set làm StartupObject.

---

### Cách 2: Build và chạy

```bash
cd samples/WSC.DataAccess.RealDB.Test
dotnet build
dotnet run
```

---

### Cách 3: Set LogTest làm startup class

Sửa `WSC.DataAccess.RealDB.Test.csproj`:

```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework>net8.0</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings>
  <Nullable>enable</Nullable>
  <StartupObject>WSC.DataAccess.RealDB.Test.LogTest</StartupObject>
</PropertyGroup>
```

Sau đó:
```bash
dotnet run
```

---

## 📋 Test Programs Available

Project có 4 test programs:

| File | Purpose | Startup Class |
|------|---------|---------------|
| **LogTest.cs** | Simple & fast log test | `WSC.DataAccess.RealDB.Test.LogTest` |
| **QuickLoggingTest.cs** | Quick verification test | `WSC.DataAccess.RealDB.Test.QuickLoggingTest` |
| **LoggingTestProgram.cs** | Comprehensive 8 test cases | `WSC.DataAccess.RealDB.Test.LoggingTestProgram` |
| **ProgramWithSqlMaps.cs** | Real database test with SQL maps | `WSC.DataAccess.RealDB.Test.ProgramWithSqlMaps` |

---

## ✅ Expected Output

Khi chạy **LogTest.cs**, bạn sẽ thấy:

```
╔══════════════════════════════════════════════════════════════════╗
║  iBatis Log Test - Simple & Fast                                ║
╚══════════════════════════════════════════════════════════════════╝

✓ Logging enabled (Debug level)

═══════════════════════════════════════════════════════════════

1️⃣  Registering SQL statement...
   ✓ Statement registered

2️⃣  Opening database session...
   ✓ Session opened

3️⃣  Starting transaction...
   ✓ Transaction started

4️⃣  Executing query...
   ✓ Query executed, got 1 row(s)

5️⃣  Committing transaction...
   ✓ Transaction committed

6️⃣  Closing session...
   ✓ Session closed

═══════════════════════════════════════════════════════════════

7️⃣  Testing error scenario...
   ✓ Error caught: Invalid object name 'TableDoesNotExist'

8️⃣  Testing warning scenario...
   ✓ Warning logged: SQL statement 'Does.Not.Exist' not found

═══════════════════════════════════════════════════════════════

✅ All tests completed!

📁 Check log files:

   Full log:  /path/to/log/iBatis/ibatis-20260120.log
   Error log: /path/to/log/iBatis/ibatis-errors-20260120.log

═══════════════════════════════════════════════════════════════
📝 Last 15 lines from log:

[...last 15 log lines with color coding...]

═══════════════════════════════════════════════════════════════
```

---

## 📁 Log Files

Logs được tạo tại:

```
samples/WSC.DataAccess.RealDB.Test/log/iBatis/
├── ibatis-20260120.log          ← Full logs
└── ibatis-errors-20260120.log   ← Errors only
```

---

## 🔍 Verify Logs

### View full log:
```bash
cat log/iBatis/ibatis-20260120.log

# Or tail in real-time
tail -f log/iBatis/ibatis-20260120.log
```

### View error log:
```bash
cat log/iBatis/ibatis-errors-20260120.log
```

### Search for specific patterns:
```bash
# Find all queries
grep "QueryAsync completed" log/iBatis/ibatis-20260120.log

# Find all errors
grep "ERR" log/iBatis/ibatis-20260120.log

# Find operations for a specific session
grep "SessionId: a1b2c3d4" log/iBatis/ibatis-20260120.log
```

---

## 📊 What You Should See in Logs

### Full Log (ibatis-YYYYMMDD.log)

```log
2026-01-20 14:30:00.100 +07:00 [DBG] WSC.DataAccess.Mapping.SqlMapConfig: Registered new statement: Test.SimpleQuery, Type: Select
2026-01-20 14:30:00.200 +07:00 [DBG] WSC.DataAccess.Core.DbSession: DbSession created - SessionId: a1b2c3d4, Database: TestDB
2026-01-20 14:30:00.210 +07:00 [DBG] WSC.DataAccess.Core.DbSession: Opening connection - SessionId: a1b2c3d4
2026-01-20 14:30:00.300 +07:00 [INF] WSC.DataAccess.Core.DbSession: Connection opened - SessionId: a1b2c3d4, Database: TestDB
2026-01-20 14:30:00.400 +07:00 [DBG] WSC.DataAccess.Core.DbSession: Beginning transaction - SessionId: a1b2c3d4
2026-01-20 14:30:00.410 +07:00 [INF] WSC.DataAccess.Core.DbSession: Transaction started - SessionId: a1b2c3d4, IsolationLevel: ReadCommitted
2026-01-20 14:30:00.500 +07:00 [DBG] WSC.DataAccess.Mapping.SqlMapper: Executing QueryAsync - StatementId: Test.SimpleQuery, Type: ExpandoObject
2026-01-20 14:30:00.634 +07:00 [INF] WSC.DataAccess.Mapping.SqlMapper: QueryAsync completed - StatementId: Test.SimpleQuery, ResultCount: 1, Duration: 134ms
2026-01-20 14:30:00.700 +07:00 [DBG] WSC.DataAccess.Core.DbSession: Committing transaction - SessionId: a1b2c3d4
2026-01-20 14:30:00.800 +07:00 [INF] WSC.DataAccess.Core.DbSession: Transaction committed successfully - SessionId: a1b2c3d4
2026-01-20 14:30:00.900 +07:00 [DBG] WSC.DataAccess.Core.DbSession: Disposing DbSession - SessionId: a1b2c3d4
2026-01-20 14:30:00.910 +07:00 [DBG] WSC.DataAccess.Core.DbSession: Closing connection - SessionId: a1b2c3d4
2026-01-20 14:30:01.000 +07:00 [INF] WSC.DataAccess.Core.DbSession: Connection closed - SessionId: a1b2c3d4
2026-01-20 14:30:01.100 +07:00 [DBG] WSC.DataAccess.Mapping.SqlMapConfig: Registered new statement: Test.ErrorQuery, Type: Select
2026-01-20 14:30:01.200 +07:00 [DBG] WSC.DataAccess.Core.DbSession: DbSession created - SessionId: e5f6g7h8, Database: TestDB
2026-01-20 14:30:01.300 +07:00 [INF] WSC.DataAccess.Core.DbSession: Connection opened - SessionId: e5f6g7h8, Database: TestDB
2026-01-20 14:30:01.400 +07:00 [DBG] WSC.DataAccess.Mapping.SqlMapper: Executing QueryAsync - StatementId: Test.ErrorQuery, Type: ExpandoObject
2026-01-20 14:30:01.500 +07:00 [ERR] WSC.DataAccess.Mapping.SqlMapper: QueryAsync failed - StatementId: Test.ErrorQuery, Duration: 100ms
System.Data.SqlClient.SqlException: Invalid object name 'TableDoesNotExist'.
   at System.Data.SqlClient.SqlCommand.ExecuteReader()
   at WSC.DataAccess.Mapping.SqlMapper.QueryAsync[T](DbSession session, String statementId, Object parameters)
2026-01-20 14:30:01.600 +07:00 [INF] WSC.DataAccess.Core.DbSession: Connection closed - SessionId: e5f6g7h8
2026-01-20 14:30:01.700 +07:00 [WRN] WSC.DataAccess.Mapping.SqlMapConfig: Statement not found: Does.Not.Exist
```

### Error Log (ibatis-errors-YYYYMMDD.log)

```log
2026-01-20 14:30:01.500 +07:00 [ERR] WSC.DataAccess.Mapping.SqlMapper: QueryAsync failed - StatementId: Test.ErrorQuery, Duration: 100ms
System.Data.SqlClient.SqlException: Invalid object name 'TableDoesNotExist'.
   at System.Data.SqlClient.SqlCommand.ExecuteReader()
   at WSC.DataAccess.Mapping.SqlMapper.QueryAsync[T](DbSession session, String statementId, Object parameters)

2026-01-20 14:30:01.700 +07:00 [WRN] WSC.DataAccess.Mapping.SqlMapConfig: Statement not found: Does.Not.Exist
```

---

## ✅ Verification Checklist

- [ ] Console shows all 8 steps completed
- [ ] Log directory exists: `log/iBatis/`
- [ ] Full log file exists and has content
- [ ] Error log file exists and has content
- [ ] Full log contains `[DBG]`, `[INF]`, `[WRN]`, `[ERR]`
- [ ] Error log only contains `[WRN]` and `[ERR]`
- [ ] SessionIds are present (e.g., `SessionId: a1b2c3d4`)
- [ ] Execution times are logged (e.g., `Duration: 134ms`)
- [ ] Exception stack traces in error log

---

## 🐛 Troubleshooting

### Problem: No log files created

**Solution**: Add Serilog packages
```bash
dotnet add package Serilog
dotnet add package Serilog.Extensions.Logging
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
```

---

### Problem: Log files empty

**Solution**: Check logging configuration in code:
```csharp
.ConfigureLogging((context, logging) =>
{
    logging.ClearProviders();
    logging.AddIBatisLogging();
})
```

---

### Problem: Database connection error

**Solution**: The test uses integrated security. Update connection string if needed:
```csharp
var connectionString = "Server=YOUR_SERVER;Database=TestDB;User Id=sa;Password=xxx;TrustServerCertificate=true;";
```

---

## 📚 Related Documentation

- `LOGGING_TEST_GUIDE.md` - Comprehensive testing guide
- `LOG_EXAMPLES.md` - Log pattern examples
- `IBATIS_LOGGING.md` - Configuration guide

---

## 🎯 Quick Start

```bash
# 1. Go to test project
cd samples/WSC.DataAccess.RealDB.Test

# 2. Run test
dotnet run

# 3. Check logs
ls -la log/iBatis/
cat log/iBatis/ibatis-*.log

# 4. Done! ✅
```

---

✅ **Happy Testing!** 🎉
