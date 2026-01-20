# iBatis Log Examples - Quick Reference

Quick reference cho các log patterns bạn sẽ thấy trong `log/iBatis/` files.

## 📋 Log Format

```
{Timestamp} [{Level}] {SourceContext}: {Message}
{Exception if any}
```

**Example**:
```
2026-01-20 14:30:15.123 +07:00 [INF] WSC.DataAccess.Core.DbSession: Connection opened - SessionId: a1b2c3d4, Database: TestDB
```

---

## 🟢 Success Scenarios

### 1. SQL Map Loading (Startup)

```log
2026-01-20 14:30:00.100 +07:00 [INF] WSC.DataAccess.Mapping.SqlMapConfig: Loading SQL map file: SqlMaps/ApplicationMap.xml
2026-01-20 14:30:00.150 +07:00 [DBG] WSC.DataAccess.Mapping.SqlMapConfig: Loaded SELECT statement: Application.GetAll
2026-01-20 14:30:00.151 +07:00 [DBG] WSC.DataAccess.Mapping.SqlMapConfig: Loaded SELECT statement: Application.GetById
2026-01-20 14:30:00.152 +07:00 [DBG] WSC.DataAccess.Mapping.SqlMapConfig: Loaded INSERT statement: Application.Insert
2026-01-20 14:30:00.200 +07:00 [INF] WSC.DataAccess.Mapping.SqlMapConfig: Successfully loaded SQL map file: SqlMaps/ApplicationMap.xml. Total statements: 5 (SELECT: 3, INSERT: 1, UPDATE: 1, DELETE: 0, PROCEDURE: 0)
```

---

### 2. Connection Lifecycle

**Opening Connection**:
```log
2026-01-20 14:30:15.100 +07:00 [DBG] WSC.DataAccess.Core.DbSession: DbSession created - SessionId: a1b2c3d4, Database: TestDB
2026-01-20 14:30:15.110 +07:00 [DBG] WSC.DataAccess.Core.DbSession: Opening connection - SessionId: a1b2c3d4
2026-01-20 14:30:15.234 +07:00 [INF] WSC.DataAccess.Core.DbSession: Connection opened - SessionId: a1b2c3d4, Database: TestDB
```

**Closing Connection**:
```log
2026-01-20 14:30:20.100 +07:00 [DBG] WSC.DataAccess.Core.DbSession: Disposing DbSession - SessionId: a1b2c3d4
2026-01-20 14:30:20.110 +07:00 [DBG] WSC.DataAccess.Core.DbSession: Closing connection - SessionId: a1b2c3d4
2026-01-20 14:30:20.200 +07:00 [INF] WSC.DataAccess.Core.DbSession: Connection closed - SessionId: a1b2c3d4
2026-01-20 14:30:20.210 +07:00 [DBG] WSC.DataAccess.Core.DbSession: DbSession disposed - SessionId: a1b2c3d4
```

---

### 3. Query Execution (Success)

**QueryAsync (Multiple Results)**:
```log
2026-01-20 14:30:16.100 +07:00 [DBG] WSC.DataAccess.Mapping.SqlMapper: Executing QueryAsync - StatementId: Application.GetAll, Type: Application
2026-01-20 14:30:16.234 +07:00 [INF] WSC.DataAccess.Mapping.SqlMapper: QueryAsync completed - StatementId: Application.GetAll, ResultCount: 15, Duration: 134ms
```

**QuerySingleAsync (Single Result)**:
```log
2026-01-20 14:30:17.100 +07:00 [DBG] WSC.DataAccess.Mapping.SqlMapper: Executing QuerySingleAsync - StatementId: Application.GetById, Type: Application
2026-01-20 14:30:17.145 +07:00 [INF] WSC.DataAccess.Mapping.SqlMapper: QuerySingleAsync completed - StatementId: Application.GetById, Found: True, Duration: 45ms
```

**ExecuteAsync (Insert/Update/Delete)**:
```log
2026-01-20 14:30:18.100 +07:00 [DBG] WSC.DataAccess.Mapping.SqlMapper: Executing ExecuteAsync - StatementId: Application.Insert
2026-01-20 14:30:18.167 +07:00 [INF] WSC.DataAccess.Mapping.SqlMapper: ExecuteAsync completed - StatementId: Application.Insert, RowsAffected: 1, Duration: 67ms
```

**ExecuteScalarAsync**:
```log
2026-01-20 14:30:19.100 +07:00 [DBG] WSC.DataAccess.Mapping.SqlMapper: Executing ExecuteScalarAsync - StatementId: Application.GetCount, Type: Int32
2026-01-20 14:30:19.123 +07:00 [INF] WSC.DataAccess.Mapping.SqlMapper: ExecuteScalarAsync completed - StatementId: Application.GetCount, Result: 42, Duration: 23ms
```

---

### 4. Transaction Lifecycle

**Begin → Commit**:
```log
2026-01-20 14:30:20.100 +07:00 [DBG] WSC.DataAccess.Core.DbSession: Beginning transaction - SessionId: a1b2c3d4
2026-01-20 14:30:20.110 +07:00 [INF] WSC.DataAccess.Core.DbSession: Transaction started - SessionId: a1b2c3d4, IsolationLevel: ReadCommitted

... SQL operations ...

2026-01-20 14:30:25.100 +07:00 [DBG] WSC.DataAccess.Core.DbSession: Committing transaction - SessionId: a1b2c3d4
2026-01-20 14:30:25.234 +07:00 [INF] WSC.DataAccess.Core.DbSession: Transaction committed successfully - SessionId: a1b2c3d4
```

**Begin → Rollback**:
```log
2026-01-20 14:30:30.100 +07:00 [DBG] WSC.DataAccess.Core.DbSession: Beginning transaction - SessionId: e5f6g7h8
2026-01-20 14:30:30.110 +07:00 [INF] WSC.DataAccess.Core.DbSession: Transaction started - SessionId: e5f6g7h8, IsolationLevel: ReadCommitted

... Error occurs ...

2026-01-20 14:30:35.100 +07:00 [DBG] WSC.DataAccess.Core.DbSession: Rolling back transaction - SessionId: e5f6g7h8
2026-01-20 14:30:35.123 +07:00 [WRN] WSC.DataAccess.Core.DbSession: Transaction rolled back - SessionId: e5f6g7h8
```

---

### 5. Repository Operations

**Success**:
```log
2026-01-20 14:30:40.100 +07:00 [DBG] WSC.DataAccess.Repository.SqlMapRepository: Repository QueryListAsync - Entity: Application, StatementId: Application.GetAll
2026-01-20 14:30:40.234 +07:00 [INF] WSC.DataAccess.Mapping.SqlMapper: QueryAsync completed - StatementId: Application.GetAll, ResultCount: 15, Duration: 134ms
```

**Transaction in Repository**:
```log
2026-01-20 14:30:45.100 +07:00 [DBG] WSC.DataAccess.Repository.SqlMapRepository: Repository ExecuteInTransactionAsync - Entity: Application
2026-01-20 14:30:45.110 +07:00 [INF] WSC.DataAccess.Core.DbSession: Transaction started - SessionId: x9y8z7w6, IsolationLevel: ReadCommitted

... Operations ...

2026-01-20 14:30:46.234 +07:00 [INF] WSC.DataAccess.Core.DbSession: Transaction committed successfully - SessionId: x9y8z7w6
2026-01-20 14:30:46.240 +07:00 [DBG] WSC.DataAccess.Repository.SqlMapRepository: Repository transaction completed successfully - Entity: Application
```

---

## 🔴 Error Scenarios

### 1. Statement Not Found

```log
2026-01-20 14:35:00.100 +07:00 [WRN] WSC.DataAccess.Mapping.SqlMapConfig: Statement not found: Application.InvalidStatement
```

**In ibatis-errors-YYYYMMDD.log**:
```log
2026-01-20 14:35:00.100 +07:00 [WRN] WSC.DataAccess.Mapping.SqlMapConfig: Statement not found: Application.InvalidStatement
```

---

### 2. SQL Execution Error

```log
2026-01-20 14:35:10.100 +07:00 [DBG] WSC.DataAccess.Mapping.SqlMapper: Executing QueryAsync - StatementId: Application.GetAll, Type: Application
2026-01-20 14:35:10.234 +07:00 [ERR] WSC.DataAccess.Mapping.SqlMapper: QueryAsync failed - StatementId: Application.GetAll, Duration: 134ms
System.Data.SqlClient.SqlException (0x80131904): Invalid column name 'ApplicationName'.
   at System.Data.SqlClient.SqlConnection.OnError(SqlException exception, Boolean breakConnection, Action`1 wrapCloseInAction)
   at System.Data.SqlClient.SqlInternalConnection.OnError(SqlException exception, Boolean breakConnection, Action`1 wrapCloseInAction)
   at WSC.DataAccess.Mapping.SqlMapper.QueryAsync[T](DbSession session, String statementId, Object parameters) in /home/user/WSC.DataAccess/src/WSC.DataAccess/Mapping/SqlMapper.cs:line 54
```

**In ibatis-errors-YYYYMMDD.log** (same content):
```log
2026-01-20 14:35:10.234 +07:00 [ERR] WSC.DataAccess.Mapping.SqlMapper: QueryAsync failed - StatementId: Application.GetAll, Duration: 134ms
System.Data.SqlClient.SqlException (0x80131904): Invalid column name 'ApplicationName'.
   [Full stack trace...]
```

---

### 3. Transaction Commit Failed

```log
2026-01-20 14:35:20.100 +07:00 [DBG] WSC.DataAccess.Core.DbSession: Committing transaction - SessionId: a1b2c3d4
2026-01-20 14:35:20.234 +07:00 [ERR] WSC.DataAccess.Core.DbSession: Transaction commit failed - SessionId: a1b2c3d4
System.InvalidOperationException: This SqlTransaction has completed; it is no longer usable.
   at System.Data.SqlClient.SqlTransaction.ZombieCheck()
   at WSC.DataAccess.Core.DbSession.Commit() in /home/user/WSC.DataAccess/src/WSC.DataAccess/Core/DbSession.cs:line 84
```

---

### 4. Repository Operation Failed

```log
2026-01-20 14:35:30.100 +07:00 [DBG] WSC.DataAccess.Repository.SqlMapRepository: Repository QuerySingleAsync - Entity: Application, StatementId: Application.GetById
2026-01-20 14:35:30.234 +07:00 [ERR] WSC.DataAccess.Mapping.SqlMapper: QuerySingleAsync failed - StatementId: Application.GetById, Duration: 134ms
System.Data.SqlClient.SqlException (0x80131904): Invalid object name 'Applications'.
   [Stack trace...]
2026-01-20 14:35:30.240 +07:00 [ERR] WSC.DataAccess.Repository.SqlMapRepository: Repository QuerySingleAsync failed - Entity: Application, StatementId: Application.GetById
System.Data.SqlClient.SqlException (0x80131904): Invalid object name 'Applications'.
   [Stack trace...]
```

---

## ⚠️ Warning Scenarios

### 1. Statement Overwrite

```log
2026-01-20 14:40:00.100 +07:00 [WRN] WSC.DataAccess.Mapping.SqlMapConfig: Overwriting existing statement: Application.GetAll, Type: Select
```

---

### 2. Transaction Not Committed Before Dispose

```log
2026-01-20 14:40:10.100 +07:00 [DBG] WSC.DataAccess.Core.DbSession: Disposing DbSession - SessionId: a1b2c3d4
2026-01-20 14:40:10.110 +07:00 [WRN] WSC.DataAccess.Core.DbSession: Transaction not committed or rolled back before dispose - SessionId: a1b2c3d4. Rolling back automatically.
```

---

## 📊 Performance Tracking

### Find Slow Queries

**In logs**:
```log
2026-01-20 14:45:00.100 +07:00 [INF] WSC.DataAccess.Mapping.SqlMapper: QueryAsync completed - StatementId: Report.GetHugeData, ResultCount: 50000, Duration: 5432ms
```

**Bash command**:
```bash
grep "Duration:" log/iBatis/ibatis-20260120.log | grep -E "Duration: [1-9][0-9]{3,}ms"
```

---

### Track Query Frequency

```bash
grep "QueryAsync completed" log/iBatis/ibatis-20260120.log | \
  awk '{print $7}' | \
  sort | \
  uniq -c | \
  sort -rn
```

**Output**:
```
    150 Application.GetAll
     89 Application.GetById
     45 Application.Search
     12 Application.Insert
```

---

## 🔍 Session Tracking

### Complete Session Lifecycle

```log
2026-01-20 14:50:00.100 +07:00 [DBG] WSC.DataAccess.Core.DbSession: DbSession created - SessionId: a1b2c3d4, Database: TestDB
2026-01-20 14:50:00.110 +07:00 [INF] WSC.DataAccess.Core.DbSession: Connection opened - SessionId: a1b2c3d4, Database: TestDB
2026-01-20 14:50:00.200 +07:00 [INF] WSC.DataAccess.Core.DbSession: Transaction started - SessionId: a1b2c3d4, IsolationLevel: ReadCommitted
2026-01-20 14:50:00.300 +07:00 [DBG] WSC.DataAccess.Mapping.SqlMapper: Executing QueryAsync - StatementId: Application.GetAll, Type: Application
2026-01-20 14:50:00.434 +07:00 [INF] WSC.DataAccess.Mapping.SqlMapper: QueryAsync completed - StatementId: Application.GetAll, ResultCount: 15, Duration: 134ms
2026-01-20 14:50:00.500 +07:00 [DBG] WSC.DataAccess.Mapping.SqlMapper: Executing ExecuteAsync - StatementId: Application.Insert
2026-01-20 14:50:00.567 +07:00 [INF] WSC.DataAccess.Mapping.SqlMapper: ExecuteAsync completed - StatementId: Application.Insert, RowsAffected: 1, Duration: 67ms
2026-01-20 14:50:01.000 +07:00 [INF] WSC.DataAccess.Core.DbSession: Transaction committed successfully - SessionId: a1b2c3d4
2026-01-20 14:50:01.100 +07:00 [INF] WSC.DataAccess.Core.DbSession: Connection closed - SessionId: a1b2c3d4
2026-01-20 14:50:01.110 +07:00 [DBG] WSC.DataAccess.Core.DbSession: DbSession disposed - SessionId: a1b2c3d4
```

**Track all operations for a session**:
```bash
grep "SessionId: a1b2c3d4" log/iBatis/ibatis-20260120.log
```

---

## 📈 Statistics Examples

### Query Performance Summary

```bash
grep "QueryAsync completed" log/iBatis/ibatis-20260120.log | \
  awk '{print $(NF-1)}' | \
  sed 's/Duration://' | \
  sed 's/ms//' | \
  awk '{sum+=$1; count++} END {print "Avg: " sum/count "ms, Total: " count " queries"}'
```

**Output**:
```
Avg: 234.5ms, Total: 156 queries
```

---

### Error Rate

```bash
total=$(grep -c "Executing" log/iBatis/ibatis-20260120.log)
errors=$(grep -c "failed" log/iBatis/ibatis-20260120.log)
echo "Error rate: $(echo "scale=2; $errors * 100 / $total" | bc)%"
```

**Output**:
```
Error rate: 2.56%
```

---

## 🎯 Quick Reference Table

| Log Level | Purpose | Full Log | Error Log |
|-----------|---------|----------|-----------|
| **[DBG]** | Debugging details | ✅ | ❌ |
| **[INF]** | Important events | ✅ | ❌ |
| **[WRN]** | Warnings | ✅ | ✅ |
| **[ERR]** | Errors | ✅ | ✅ |

---

## 🔗 Related Files

- `IBATIS_LOGGING.md` - Full logging configuration guide
- `LOGGING_TEST_GUIDE.md` - How to run tests
- `QuickLoggingTest.cs` - Quick test program
- `LoggingTestProgram.cs` - Comprehensive test suite

---

✅ **Use this as a quick reference when analyzing logs!**
