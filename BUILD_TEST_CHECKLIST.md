# Build & Test Checklist

## ✅ Pre-Build Verification

### 1. Files Structure
```
WSC.DataAccess/
├── src/WSC.DataAccess/
│   ├── Core/
│   │   ├── ISql.cs ✓
│   │   ├── ISqlMapConnection.cs ✓
│   │   ├── SqlMapConnection.cs ✓
│   │   └── SqlService.cs ✓
│   ├── Extensions/
│   │   └── SqlConnectionExtensions.cs ✓
│   ├── Configuration/
│   │   ├── SqlMapProvider.cs ✓
│   │   └── DataAccessServiceCollectionExtensions.cs ✓
│   └── Repository/
│       ├── ProviderBasedRepository.cs ✓
│       └── MultiDaoProviderRepository.cs ✓
│
├── samples/WSC.DataAccess.Sample/
│   ├── Models/
│   │   └── Provider.cs ✓
│   ├── Services/
│   │   ├── SimpleUserService.cs ✓
│   │   ├── ComplexBusinessService.cs ✓
│   │   └── AssetDbService.cs ✓
│   └── Repositories/ (5 files) ✓
│
└── Documentation ✓
    ├── DAONAMES_MAPPING_GUIDE.md
    ├── DAONAMES_PATTERN_README.md
    ├── MULTIPLE_CONNECTIONS_DAONAMES.md
    └── ISQL_PATTERN_GUIDE.md
```

---

## 🔨 Build Commands

### Build Main Library
```bash
cd /home/user/WSC.DataAccess
dotnet build src/WSC.DataAccess/WSC.DataAccess.csproj
```

**Expected**: ✅ Build succeeded, 0 errors

### Build Sample Project
```bash
dotnet build samples/WSC.DataAccess.Sample/WSC.DataAccess.Sample.csproj
```

**Expected**: ✅ Build succeeded, 0 errors

### Build Entire Solution
```bash
dotnet build WSC.DataAccess.sln
```

**Expected**: ✅ Build succeeded, 0 errors

---

## 🧪 Test Cases

### Test 1: ISql Pattern - Basic CRUD

**File**: Create `tests/ISqlPatternTests.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using WSC.DataAccess.Core;
using WSC.DataAccess.Extensions;
using Xunit;

public class ISqlPatternTests
{
    [Fact]
    public void ISql_ShouldBeRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddWscDataAccess("Server=localhost;Database=Test;", options =>
        {
            options.ConfigureSqlMaps(provider =>
            {
                provider.AddFile("DAO000", "SqlMaps/DAO000.xml");
            });
        });
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var sql = serviceProvider.GetService<ISql>();

        // Assert
        Assert.NotNull(sql);
    }

    [Fact]
    public void GetDAO_ShouldSetCurrentDao()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddWscDataAccess("Server=localhost;Database=Test;", options =>
        {
            options.ConfigureSqlMaps(provider =>
            {
                provider.AddFile("DAO000", "SqlMaps/DAO000.xml");
            });
        });
        var serviceProvider = services.BuildServiceProvider();
        var sql = serviceProvider.GetRequiredService<ISql>();

        // Act
        sql.GetDAO("DAO000");

        // Assert
        Assert.Equal("DAO000", sql.CurrentDao);
    }

    [Fact]
    public void CreateConnection_WithoutGetDAO_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddWscDataAccess("Server=localhost;Database=Test;", options =>
        {
            options.ConfigureSqlMaps(provider =>
            {
                provider.AddFile("DAO000", "SqlMaps/DAO000.xml");
            });
        });
        var serviceProvider = services.BuildServiceProvider();
        var sql = serviceProvider.GetRequiredService<ISql>();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => sql.CreateConnection());
    }
}
```

**Run**:
```bash
dotnet test
```

**Expected**: ✅ All tests passed

---

### Test 2: Provider Pattern - DaoNames Mapping

**File**: Create `tests/ProviderPatternTests.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using WSC.DataAccess.Configuration;
using Xunit;

public class ProviderPatternTests
{
    [Fact]
    public void SqlMapProvider_ShouldRegisterDaos()
    {
        // Arrange
        var provider = new SqlMapProvider();

        // Act
        provider.AddFile("DAO000", "SqlMaps/DAO000.xml", "Test DAO");
        provider.AddFile("DAO001", "SqlMaps/DAO001.xml", "Test DAO 2");

        // Assert
        Assert.True(provider.HasFile("DAO000"));
        Assert.True(provider.HasFile("DAO001"));
        Assert.Equal("SqlMaps/DAO000.xml", provider.GetFilePath("DAO000"));
    }

    [Fact]
    public void SqlMapProvider_ShouldSupportMultipleConnections()
    {
        // Arrange
        var provider = new SqlMapProvider();

        // Act
        provider.AddFile("DAO000", "SqlMaps/DAO000.xml", "MainDB");
        provider.AddFile("DAO000", "SqlMaps/DAO000.xml", "ReportDB");

        // Assert
        Assert.True(provider.HasFile("DAO000", "MainDB"));
        Assert.True(provider.HasFile("DAO000", "ReportDB"));
    }
}
```

---

### Test 3: Extension Methods

**Manual Test**:

```csharp
// Setup
var services = new ServiceCollection();
services.AddWscDataAccess(connectionString, options =>
{
    options.ConfigureSqlMaps(provider =>
    {
        provider.AddFile(Provider.DAO000, "SqlMaps/DAO000.xml");
    });
});
var serviceProvider = services.BuildServiceProvider();
var sql = serviceProvider.GetRequiredService<ISql>();

// Test 1: StatementExecuteQueryAsync
sql.GetDAO(Provider.DAO000);
using var conn = sql.CreateConnection();
var results = await conn.StatementExecuteQueryAsync<Asset>("Asset.GetAll");
Assert.NotNull(results);

// Test 2: StatementExecuteScalarAsync
var count = await conn.StatementExecuteScalarAsync<int>("Asset.Count");
Assert.True(count >= 0);

// Test 3: StatementExecuteAsync
var rowsAffected = await conn.StatementExecuteAsync("Asset.Insert", new Asset { ... });
Assert.True(rowsAffected > 0);

// Test 4: Transaction
await conn.ExecuteInTransactionAsync(async c =>
{
    await c.StatementExecuteAsync("Asset.Insert", asset1);
    await c.StatementExecuteAsync("Asset.Insert", asset2);
});
```

---

## 📋 Integration Test Checklist

### ✅ ISql Pattern

- [ ] `ISql` is registered in DI container
- [ ] `GetDAO()` sets current DAO context
- [ ] `CreateConnection()` returns `ISqlMapConnection`
- [ ] Extension methods work:
  - [ ] `StatementExecuteQueryAsync<T>()`
  - [ ] `StatementExecuteSingleAsync<T>()`
  - [ ] `StatementExecuteScalarAsync<T>()`
  - [ ] `StatementExecuteAsync()`
  - [ ] `ExecuteInTransactionAsync()`

### ✅ Provider Pattern

- [ ] `SqlMapProvider` registers DAO mappings
- [ ] `ConfigureSqlMaps()` callback works in Program.cs
- [ ] DaoNames mapping works (Provider.DAO000 → file path)
- [ ] Multiple connections supported (MainDB, ReportDB)

### ✅ Repository Pattern

- [ ] `ProviderBasedRepository<T>` works with single DAO
- [ ] `MultiDaoProviderRepository<T>` works with multiple DAOs
- [ ] Cross-domain queries work
- [ ] Transactions work

### ✅ Sample Project

- [ ] `CompleteDemo.cs` compiles
- [ ] All sample repositories compile:
  - [ ] SystemRepository
  - [ ] UserRepository
  - [ ] ProductRepository
  - [ ] OrderRepository
  - [ ] ReportRepository
- [ ] Sample services compile:
  - [ ] SimpleUserService
  - [ ] ComplexBusinessService
  - [ ] AssetDbService

---

## 🐛 Common Issues & Solutions

### Issue 1: "Statement not found in SQL map"
**Cause**: Statement ID mismatch between code and XML
**Solution**: Check SQL map XML file has correct `<select id="StatementId">`

### Issue 2: "DAO not found in provider"
**Cause**: DAO not registered in `ConfigureSqlMaps()`
**Solution**: Add `provider.AddFile(Provider.DAO000, "SqlMaps/DAO000.xml");`

### Issue 3: "No DAO context set"
**Cause**: Forgot to call `_sql.GetDAO()` before `CreateConnection()`
**Solution**: Always call `_sql.GetDAO(Provider.DAO000);` first

### Issue 4: Connection leak
**Cause**: Not using `using` statement
**Solution**: Always use `using var connection = _sql.CreateConnection();`

---

## ✅ Final Verification

### Step 1: Clean Build
```bash
dotnet clean
dotnet build
```
**Expected**: 0 errors, 0 warnings

### Step 2: Run Sample
```bash
cd samples/WSC.DataAccess.Sample
dotnet run
```
**Expected**: Demo runs without errors

### Step 3: Test Pattern
Create a simple test project and verify all 3 patterns work:
1. ✅ ISql pattern
2. ✅ Repository pattern (single DAO)
3. ✅ Repository pattern (multiple DAOs)

---

## 📊 Success Criteria

| Criterion | Status | Notes |
|-----------|--------|-------|
| Project compiles | ⬜ | 0 errors |
| All tests pass | ⬜ | Unit + Integration |
| ISql pattern works | ⬜ | Basic CRUD |
| Provider pattern works | ⬜ | DaoNames mapping |
| Repository pattern works | ⬜ | Single + Multiple DAOs |
| Multiple connections work | ⬜ | MainDB + ReportDB |
| Transaction support works | ⬜ | Commit + Rollback |
| Documentation complete | ✅ | 4 guides created |
| Sample code works | ⬜ | CompleteDemo runs |

---

## 🎯 Next Steps

1. **Build project**: Run `dotnet build`
2. **Fix any errors**: Check error messages, fix code
3. **Run tests**: Run `dotnet test`
4. **Test manually**: Create simple console app to test patterns
5. **Deploy**: Publish NuGet package if needed

---

## 📞 Support

**Issues found?**
- Check documentation: ISQL_PATTERN_GUIDE.md
- Check examples: samples/WSC.DataAccess.Sample/
- Review commit history: git log --oneline

**All commits**:
- `301f120` - Add DaoNames mapping pattern
- `968b8a3` - Add ISql pattern + cleanup
- `a391496` - Fix compile errors (SqlConnection → SqlMapConnection)
- `a663e1c` - Rename ISqlConnection → ISqlMapConnection
- `55048be` - Fix SqlService return types

**Ready to test! 🚀**
