# Multiple Connection Strings Guide

## Giải thích Connection Names

### 1. Connection Names trong appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "...",  // Tên: "DefaultConnection" → Map: "Default"
    "HISConnection": "...",      // Tên: "HISConnection"    → Map: "HIS"
    "LISConnection": "..."       // Tên: "LISConnection"    → Map: "LIS"
  }
}
```

### 2. Mapping Logic

**DefaultConnection** → Tự động dùng cho connection name **"Default"**

**Named Connections** → Bạn tự đặt tên khi register:

```csharp
// Đọc connection string từ appsettings.json
var hisConnection = configuration.GetConnectionString("HISConnection");

// Register với tên "HIS" (bạn tự đặt)
options.AddConnection("HIS", hisConnection);
```

**Lưu ý:** Tên connection trong code **KHÔNG** phải trùng với tên trong appsettings.json!
- `appsettings.json`: Dùng tên như "HISConnection", "LISConnection"
- `Code`: Dùng tên ngắn gọn như "HIS", "LIS"

---

## 3 Cách Sử Dụng Multiple Connections

### Cách 1: Auto-Discovery cho Default Connection

**Đơn giản nhất** - Tất cả DAOs dùng Default connection:

```csharp
services.AddWscDataAccess(defaultConnection!, options =>
{
    // Auto-discover tất cả .xml files trong SqlMaps/
    // Mặc định dùng connection "Default"
    options.AutoDiscoverSqlMaps("SqlMaps");
});
```

**Kết quả:**
```
DAO000 → Default connection
DAO001 → Default connection
DAO002 → Default connection
...
```

---

### Cách 2: Auto-Discovery cho Từng Connection

**Chia theo thư mục** - Mỗi thư mục map với 1 connection:

```csharp
services.AddWscDataAccess(defaultConnection!, options =>
{
    // Register additional connections
    options.AddConnection("HIS", hisConnection);
    options.AddConnection("LIS", lisConnection);

    // Auto-discover theo thư mục
    options.AutoDiscoverSqlMaps("SqlMaps/Default", "Default");
    options.AutoDiscoverSqlMaps("SqlMaps/HIS", "HIS");
    options.AutoDiscoverSqlMaps("SqlMaps/LIS", "LIS");
});
```

**Cấu trúc thư mục:**
```
SqlMaps/
├── Default/
│   ├── DAO000.xml
│   └── DAO001.xml
├── HIS/
│   ├── DAO002.xml
│   └── DAO003.xml
└── LIS/
    ├── DAO004.xml
    └── DAO005.xml
```

**Kết quả:**
```
DAO000 (Default/) → Default connection
DAO001 (Default/) → Default connection
DAO002 (HIS/)     → HIS connection
DAO003 (HIS/)     → HIS connection
DAO004 (LIS/)     → LIS connection
DAO005 (LIS/)     → LIS connection
```

---

### Cách 3: Manual Registration (Full Control)

**Kiểm soát từng file** - Chọn connection cho từng DAO:

```csharp
services.AddWscDataAccess(defaultConnection!, options =>
{
    // Register additional connections
    options.AddConnection("HIS", hisConnection);
    options.AddConnection("LIS", lisConnection);

    // Manual registration với connection cụ thể
    options.ConfigureSqlMaps(provider =>
    {
        // Default connection
        provider.AddFile(Provider.DAO000, "SqlMaps/DAO000.xml", "Default");
        provider.AddFile(Provider.DAO001, "SqlMaps/DAO001.xml", "Default");

        // HIS connection
        provider.AddFile(Provider.DAO002, "SqlMaps/DAO002.xml", "HIS");
        provider.AddFile(Provider.DAO003, "SqlMaps/DAO003.xml", "HIS");

        // LIS connection
        provider.AddFile(Provider.DAO004, "SqlMaps/DAO004.xml", "LIS");
        provider.AddFile(Provider.DAO005, "SqlMaps/DAO005.xml", "LIS");
    });
});
```

---

## Cách Sử Dụng Connection trong Code

### 1. Dùng Default Connection

```csharp
_sql.GetDAO(Provider.DAO001);
using var connection = _sql.CreateConnection(); // Tự động dùng Default
```

### 2. Dùng Named Connection

```csharp
_sql.GetDAO(Provider.DAO002);
using var connection = _sql.CreateConnection("HIS"); // Chỉ định "HIS" connection
```

Hoặc dùng cả 2 trong 1 lần:

```csharp
_sql.GetDAO(Provider.DAO002, "HIS"); // DAO + Connection name
using var connection = _sql.CreateConnection();
```

---

## Ví Dụ Thực Tế

### Scenario: Một hệ thống có 3 databases

1. **Main DB**: Chứa users, products, orders
2. **HIS DB**: Hệ thống bệnh viện (Hospital Information System)
3. **LIS DB**: Hệ thống xét nghiệm (Laboratory Information System)

### Code Sample:

```csharp
public class MultiDatabaseService
{
    private readonly ISql _sql;

    public async Task<User> GetUserFromMainDB(int userId)
    {
        _sql.GetDAO(Provider.DAO001); // DAO001 map to Default
        using var conn = _sql.CreateConnection();
        return await conn.StatementExecuteSingleAsync<User>("User.GetById", new { userId });
    }

    public async Task<Patient> GetPatientFromHIS(int patientId)
    {
        _sql.GetDAO(Provider.DAO002, "HIS"); // DAO002 map to HIS
        using var conn = _sql.CreateConnection();
        return await conn.StatementExecuteSingleAsync<Patient>("Patient.GetById", new { patientId });
    }

    public async Task<LabResult> GetLabResultFromLIS(int testId)
    {
        _sql.GetDAO(Provider.DAO005, "LIS"); // DAO005 map to LIS
        using var conn = _sql.CreateConnection();
        return await conn.StatementExecuteSingleAsync<LabResult>("Lab.GetResult", new { testId });
    }
}
```

---

## Summary - Chọn Pattern Nào?

| Pattern | Khi Nào Dùng | Ưu Điểm | Nhược Điểm |
|---------|-------------|---------|------------|
| **Auto-Discovery (Default)** | 1 database duy nhất | Đơn giản nhất, không cần config | Chỉ support 1 connection |
| **Auto-Discovery (Multi-folder)** | Nhiều databases, phân chia rõ ràng | Tự động, dễ maintain theo thư mục | Phải tổ chức thư mục đúng |
| **Manual Registration** | Cần kiểm soát chính xác từng DAO | Full control, flexible | Phải khai báo thủ công, dài dòng |

**Recommendation:**
- Dùng **Cách 1** nếu chỉ có 1 database
- Dùng **Cách 2** nếu có nhiều databases và có thể chia thư mục rõ ràng
- Dùng **Cách 3** nếu cần conditional logic hoặc dynamic registration
