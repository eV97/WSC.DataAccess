# Architecture - WSC.DataAccess

Tài liệu kiến trúc và cấu trúc dự án WSC.DataAccess.

## Mục lục

- [Project Structure](#project-structure)
- [Component Architecture](#component-architecture)
- [Core Components](#core-components)
- [SQL Map System](#sql-map-system)
- [Dependency Injection](#dependency-injection)
- [Data Flow](#data-flow)
- [Design Patterns](#design-patterns)

---

## Project Structure

```
WSC.DataAccess/
├── src/
│   └── WSC.DataAccess/
│       ├── Attributes/          # Custom attributes
│       ├── Configuration/       # DI configuration & options
│       │   ├── ServiceCollectionExtensions.cs
│       │   ├── SqlMapOptions.cs
│       │   └── SqlMapProvider.cs
│       ├── Core/               # Core interfaces & implementations
│       │   ├── ISql.cs         # Main service interface
│       │   ├── SqlService.cs   # ISql implementation
│       │   ├── ISqlMapConnection.cs
│       │   ├── SqlMapConnection.cs
│       │   ├── IDbConnectionFactory.cs
│       │   ├── SqlConnectionFactory.cs
│       │   ├── IDbSessionFactory.cs
│       │   └── DbSessionFactory.cs
│       ├── Extensions/         # Extension methods
│       │   └── SqlConnectionExtensions.cs
│       ├── Mapping/            # SQL Map parsing & management
│       │   ├── SqlMapConfig.cs
│       │   ├── SqlMapConfigBuilder.cs
│       │   ├── SqlMapper.cs
│       │   └── SqlStatement.cs
│       ├── Repository/         # Repository base classes
│       │   ├── IRepository.cs
│       │   ├── BaseRepository.cs
│       │   ├── SqlMapRepository.cs
│       │   ├── SimpleSqlMapRepository.cs
│       │   ├── ScopedSqlMapRepository.cs
│       │   ├── ProviderBasedRepository.cs
│       │   └── MultiDaoProviderRepository.cs
│       └── Utilities/          # Helper classes
│
├── samples/
│   └── WSC.DataAccess.Sample/
│       ├── Models/             # Model classes
│       │   ├── User.cs
│       │   └── Provider.cs    # DAO names constants
│       ├── SqlMaps/           # SQL Map XML files
│       │   ├── DAO000.xml     # System queries
│       │   ├── DAO001.xml     # User management
│       │   ├── DAO002.xml     # Product management
│       │   └── ...
│       ├── TestService.cs     # Test service với examples
│       ├── Program.cs         # Main demo (ConfigurationSection)
│       ├── Program_Configs.cs # IConfiguration demo
│       ├── Program_Dictionary.cs # Dictionary demo
│       ├── Program_Alternative.cs # Provider.GetDaoFiles() demo
│       └── appsettings.json
│
├── database/                  # Database scripts
│   └── setup.sql
│
└── README.md
```

---

## Component Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Application Layer                       │
│  (Controllers, Services, Repositories)                      │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ Inject ISql
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                    WSC.DataAccess Layer                     │
│                                                              │
│  ┌──────────────┐     ┌──────────────────┐                 │
│  │     ISql     │────▶│ ISqlMapConnection│                 │
│  └──────────────┘     └──────────────────┘                 │
│         │                      │                            │
│         │                      │                            │
│         ▼                      ▼                            │
│  ┌──────────────┐     ┌──────────────────┐                 │
│  │ SqlService   │     │SqlMapConnection  │                 │
│  └──────────────┘     └──────────────────┘                 │
│         │                      │                            │
│         │                      │                            │
│         ▼                      ▼                            │
│  ┌──────────────┐     ┌──────────────────┐                 │
│  │DbConnectionF.│     │  SqlMapConfig    │                 │
│  └──────────────┘     └──────────────────┘                 │
│         │                      │                            │
└─────────┼──────────────────────┼──────────────────────────┘
          │                      │
          │                      │ Parses XML
          ▼                      ▼
┌─────────────────────────────────────────────────────────────┐
│                    SQL Server Database                      │
└─────────────────────────────────────────────────────────────┘
          ▲                      ▲
          │                      │
     SQL Queries          SQL Map Files (XML)
```

---

## Core Components

### 1. ISql - Main Service Interface

**Purpose:** Entry point cho data access - Quản lý DAO switching và connection creation.

**Responsibilities:**
- Tạo database connections
- Chuyển đổi DAO context
- Quản lý named connections
- Track current DAO và connection

**Location:** `src/WSC.DataAccess/Core/ISql.cs`

**Key Methods:**
- `CreateConnection()` - Tạo default connection
- `CreateConnection(string)` - Tạo named connection
- `GetDAO(string)` - Switch DAO context

---

### 2. SqlService - ISql Implementation

**Purpose:** Implementation của ISql interface.

**Responsibilities:**
- Maintain DAO-to-SqlMap mapping
- Maintain connection string registry
- Create SqlMapConnection instances
- Thread-safe DAO switching (AsyncLocal)

**Location:** `src/WSC.DataAccess/Core/SqlService.cs`

**Key Features:**
- Thread-safe context management với `AsyncLocal<T>`
- Lazy loading của SQL Map configs
- Connection string validation

---

### 3. ISqlMapConnection

**Purpose:** Database connection wrapper với SQL Map capabilities.

**Responsibilities:**
- Wrap `IDbConnection` với SqlMap context
- Provide access to SqlMapConfig
- Support extension methods
- Implement `IDbConnection` interface

**Location:** `src/WSC.DataAccess/Core/ISqlMapConnection.cs`

**Key Properties:**
- `SqlMapConfig` - SQL map configuration
- `InnerConnection` - Raw ADO.NET connection
- `DaoName` - Associated DAO name
- `ConnectionName` - Connection identifier

---

### 4. SqlConnectionExtensions

**Purpose:** Extension methods cho ISqlMapConnection - iBatis.NET style API.

**Responsibilities:**
- Provide fluent API cho query execution
- Map statement IDs to SQL text
- Execute queries using Dapper
- Transaction management

**Location:** `src/WSC.DataAccess/Extensions/SqlConnectionExtensions.cs`

**Key Methods:**
- `StatementExecuteQueryAsync<T>` - Query list
- `StatementExecuteSingleAsync<T>` - Query single
- `StatementExecuteScalarAsync<T>` - Scalar query
- `StatementExecuteAsync` - Non-query (INSERT/UPDATE/DELETE)
- `ExecuteInTransactionAsync` - Transaction wrapper

---

## SQL Map System

### SqlMapConfig

**Purpose:** Configuration object chứa parsed SQL statements từ XML file.

**Structure:**

```csharp
public class SqlMapConfig
{
    public string Namespace { get; set; }
    public Dictionary<string, SqlStatement> Statements { get; set; }

    public SqlStatement? GetStatement(string id)
    {
        return Statements.TryGetValue(id, out var statement)
            ? statement
            : null;
    }
}
```

**Location:** `src/WSC.DataAccess/Mapping/SqlMapConfig.cs`

---

### SqlStatement

**Purpose:** Represents một SQL statement từ XML.

**Structure:**

```csharp
public class SqlStatement
{
    public string Id { get; set; }           // e.g., "User.GetAll"
    public string CommandText { get; set; }  // SQL query
    public string CommandType { get; set; }  // select/insert/update/delete
    public string? ResultClass { get; set; } // Result type
}
```

**Location:** `src/WSC.DataAccess/Mapping/SqlStatement.cs`

---

### SqlMapConfigBuilder

**Purpose:** Parse XML file thành SqlMapConfig object.

**Responsibilities:**
- Load và parse XML file
- Extract namespace
- Parse `<select>`, `<insert>`, `<update>`, `<delete>` tags
- Extract CDATA content
- Build SqlMapConfig object

**Location:** `src/WSC.DataAccess/Mapping/SqlMapConfigBuilder.cs`

**XML Structure:**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<sqlMap namespace="User">
  <select id="User.GetAll" resultClass="User">
    <![CDATA[
      SELECT Id, Username, FullName, Email
      FROM Users
      ORDER BY FullName
    ]]>
  </select>

  <insert id="User.Insert">
    <![CDATA[
      INSERT INTO Users (Username, FullName, Email)
      VALUES (@Username, @FullName, @Email)
    ]]>
  </insert>
</sqlMap>
```

---

## Dependency Injection

### Service Registration Flow

```
AddWscDataAccess()
    │
    ├─▶ Parse Configuration
    │   ├─ Extract connection strings
    │   └─ Build connection string dictionary
    │
    ├─▶ Register Core Services
    │   ├─ IDbConnectionFactory (Singleton)
    │   ├─ IDbSessionFactory (Scoped)
    │   └─ ISql (Scoped)
    │
    └─▶ Configure SQL Maps
        ├─ Auto-discover XML files (if enabled)
        ├─ Parse XML to SqlMapConfig
        └─ Register DAO mappings
```

### Service Lifetimes

| Service | Lifetime | Reason |
|---------|----------|--------|
| `IDbConnectionFactory` | Singleton | Stateless factory, connection strings readonly |
| `ISql` | Scoped | Maintains DAO context per request/scope |
| `IDbSessionFactory` | Scoped | Creates connections within scope |
| `ISqlMapConnection` | Transient | Created per use, disposed explicitly |

---

## Data Flow

### Query Execution Flow

```
1. User Code
   └─▶ _sql.GetDAO("DAO001")
       └─▶ Sets current DAO context (thread-safe)

2. User Code
   └─▶ var conn = _sql.CreateConnection()
       └─▶ SqlService creates SqlMapConnection
           ├─ Loads SqlMapConfig for DAO001
           ├─ Gets connection string
           └─ Creates ADO.NET connection

3. User Code
   └─▶ await conn.StatementExecuteQueryAsync<User>("User.GetAll")
       │
       ├─▶ Extension method gets SqlMapConfig
       │
       ├─▶ Looks up statement ID "User.GetAll"
       │   └─▶ Returns SqlStatement with SQL text
       │
       ├─▶ Executes SQL using Dapper
       │   └─▶ conn.InnerConnection.QueryAsync<User>(sql, parameters)
       │
       └─▶ Returns IEnumerable<User>

4. User Code
   └─▶ conn.Dispose()
       └─▶ Closes database connection
```

---

### Transaction Flow

```
1. User Code
   └─▶ await conn.ExecuteInTransactionAsync(async c => { ... })

2. Extension Method
   ├─▶ Begin Transaction
   │   └─▶ using var transaction = conn.BeginTransaction()
   │
   ├─▶ Execute Action
   │   └─▶ await action(conn)
   │       ├─ User executes multiple statements
   │       └─ All use same connection + transaction
   │
   ├─▶ Success? → Commit
   │   └─▶ transaction.Commit()
   │
   └─▶ Exception? → Rollback
       └─▶ transaction.Rollback()
       └─▶ Re-throw exception
```

---

## Design Patterns

### 1. Factory Pattern

**DbConnectionFactory** - Tạo database connections.

```csharp
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection(string connectionName);
}
```

**Benefits:**
- Centralized connection creation
- Easy to swap database providers
- Connection string management

---

### 2. Service Locator Pattern

**SqlService** - Locate SQL Map configs by DAO name.

```csharp
public class SqlService : ISql
{
    private readonly Dictionary<string, SqlMapConfig> _sqlMapConfigs;

    public void GetDAO(string daoName)
    {
        if (!_sqlMapConfigs.ContainsKey(daoName))
            throw new InvalidOperationException($"DAO '{daoName}' not found");

        _currentDao.Value = daoName;
    }
}
```

---

### 3. Repository Pattern

**BaseRepository** - Base class cho repositories.

```csharp
public abstract class BaseRepository
{
    protected readonly ISql _sql;
    protected abstract string DaoName { get; }

    protected async Task<IEnumerable<T>> QueryAsync<T>(string statementId, object? parameters = null)
    {
        _sql.GetDAO(DaoName);
        using var conn = _sql.CreateConnection();
        return await conn.StatementExecuteQueryAsync<T>(statementId, parameters);
    }
}
```

**Benefits:**
- Encapsulate data access logic
- Reusable query patterns
- Easy testing with mocks

---

### 4. Decorator Pattern

**SqlMapConnection** wraps `IDbConnection` với additional functionality.

```csharp
public class SqlMapConnection : ISqlMapConnection
{
    private readonly IDbConnection _innerConnection;
    private readonly SqlMapConfig _sqlMapConfig;

    // Delegates all IDbConnection methods to _innerConnection
    // Adds SqlMapConfig property
}
```

---

### 5. Extension Method Pattern

**SqlConnectionExtensions** - Fluent API cho statement execution.

```csharp
public static async Task<IEnumerable<T>> StatementExecuteQueryAsync<T>(
    this ISqlMapConnection connection,
    string statementId,
    object? parameters = null)
{
    // Implementation
}
```

**Benefits:**
- Clean fluent API
- Non-intrusive (không modify original classes)
- Easy to discover with IntelliSense

---

### 6. Provider Pattern

**Provider class** - Centralized DAO names management.

```csharp
public static class Provider
{
    public const string DAO000 = "DAO000";
    public const string DAO001 = "DAO001";

    public static string[] GetDaoFiles(string baseDirectory) { ... }
    public static string GetDescription(string daoName) { ... }
}
```

**Benefits:**
- Type-safe DAO names
- Centralized management
- Easy refactoring
- IntelliSense support

---

## Technology Stack

### Core Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.Extensions.DependencyInjection` | 6.0+ | DI container |
| `Microsoft.Extensions.Configuration` | 6.0+ | Configuration management |
| `Microsoft.Extensions.Logging` | 6.0+ | Logging framework |
| `Dapper` | 2.0+ | Micro ORM for SQL execution |
| `System.Data.SqlClient` | 4.8+ | SQL Server provider |

---

## Thread Safety

### AsyncLocal<T> for DAO Context

```csharp
public class SqlService : ISql
{
    private readonly AsyncLocal<string?> _currentDao = new();
    private readonly AsyncLocal<string> _currentConnection = new();

    public void GetDAO(string daoName)
    {
        _currentDao.Value = daoName;  // Thread-safe, async-safe
    }
}
```

**Why AsyncLocal:**
- Thread-safe trong async contexts
- Isolated per async flow
- Không leak context giữa requests

**Alternative considered:**
- `ThreadLocal<T>` - ❌ Không safe với async/await
- `ThreadStatic` - ❌ Không safe với async/await
- Global state - ❌ Not thread-safe

---

## Performance Considerations

### 1. Connection Pooling

ADO.NET tự động pool connections - Không cần custom implementation.

```csharp
// Connection string with pooling (default: enabled)
"Server=.;Database=MyDB;User Id=sa;Password=***;Pooling=true;Max Pool Size=100"
```

---

### 2. Lazy Loading SQL Maps

SQL Map configs chỉ parse khi cần:

```csharp
private SqlMapConfig GetOrLoadSqlMapConfig(string daoName)
{
    if (!_sqlMapConfigs.TryGetValue(daoName, out var config))
    {
        // Load and parse XML only when needed
        config = LoadSqlMapConfig(daoName);
        _sqlMapConfigs[daoName] = config;
    }
    return config;
}
```

---

### 3. Dapper Performance

Dapper cached compiled expressions cho type mapping - Rất nhanh.

- First call: ~100ms (compile expressions)
- Subsequent calls: <1ms (cached)

---

## Extension Points

### 1. Custom Repository Base Class

Extend `BaseRepository` để add custom functionality.

---

### 2. Custom SQL Map Provider

Implement custom logic để load SQL Maps từ database, API, etc.

---

### 3. Custom Connection Factory

Implement `IDbConnectionFactory` để support other databases (PostgreSQL, MySQL, etc.).

---

## See Also

- [API Reference](API_REFERENCE.md) - Complete API documentation
- [Examples](EXAMPLES.md) - Practical examples
- [Configuration](CONFIGURATION.md) - Configuration guide
- [Getting Started](GETTING_STARTED.md) - Quick start guide
