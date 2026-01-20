# Hướng dẫn Sử dụng IBatis-style SQL Mapping

Tài liệu chi tiết về cách sử dụng pattern IBatis-style trong WSC.DataAccess.

## Mục lục

1. [Giới thiệu về IBatis Pattern](#giới-thiệu-về-ibatis-pattern)
2. [Cấu trúc SQL Map XML](#cấu-trúc-sql-map-xml)
3. [Tạo Repository với SqlMapRepository](#tạo-repository-với-sqlmaprepository)
4. [Các loại Statement](#các-loại-statement)
5. [Ví dụ Thực tế](#ví-dụ-thực-tế)
6. [Best Practices](#best-practices)

## Giới thiệu về IBatis Pattern

IBatis (nay là MyBatis) là một persistence framework cho phép tách biệt SQL queries khỏi code. WSC.DataAccess implement pattern tương tự với những ưu điểm:

### Ưu điểm:
- ✅ SQL được quản lý tập trung trong XML files
- ✅ Dễ dàng review và optimize SQL
- ✅ DBA có thể làm việc trực tiếp với SQL files
- ✅ Tái sử dụng queries giữa các projects
- ✅ Version control tốt hơn cho SQL changes

### Khi nào nên dùng:
- Complex queries với nhiều JOINs
- Stored procedures
- Dynamic queries
- Projects lớn với nhiều queries
- Team có DBA riêng

## Cấu trúc SQL Map XML

### Cấu trúc cơ bản

```xml
<?xml version="1.0" encoding="utf-8" ?>
<sqlMap namespace="EntityName">

  <!-- SELECT statements -->
  <select id="EntityName.QueryId" resultType="Full.Type.Name">
    SELECT * FROM TableName WHERE Id = @Id
  </select>

  <!-- INSERT statements -->
  <insert id="EntityName.Insert">
    INSERT INTO TableName (Column1, Column2) VALUES (@Param1, @Param2)
  </insert>

  <!-- UPDATE statements -->
  <update id="EntityName.Update">
    UPDATE TableName SET Column1 = @Param1 WHERE Id = @Id
  </update>

  <!-- DELETE statements -->
  <delete id="EntityName.Delete">
    DELETE FROM TableName WHERE Id = @Id
  </delete>

</sqlMap>
```

### Attributes

| Attribute | Bắt buộc | Mô tả | Ví dụ |
|-----------|----------|-------|-------|
| `id` | Có | Unique identifier cho statement | `Product.GetById` |
| `resultType` | Không | Type của object trả về (cho SELECT) | `MyApp.Models.Product` |
| `parameterType` | Không | Type của parameter object | `MyApp.DTOs.SearchRequest` |
| `timeout` | Không | Command timeout (seconds) | `30` |

## Tạo Repository với SqlMapRepository

### Bước 1: Tạo SQL Map File

Tạo file `ProductMap.xml` trong folder `SqlMaps/`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<sqlMap namespace="Product">

  <select id="Product.GetAll" resultType="MyApp.Models.Product">
    SELECT
      Id, ProductCode, ProductName, Description, Price,
      StockQuantity, Category, CreatedDate, UpdatedDate, IsActive
    FROM Products
    WHERE IsActive = 1
    ORDER BY ProductName
  </select>

  <select id="Product.GetById" resultType="MyApp.Models.Product">
    SELECT
      Id, ProductCode, ProductName, Description, Price,
      StockQuantity, Category, CreatedDate, UpdatedDate, IsActive
    FROM Products
    WHERE Id = @Id
  </select>

  <select id="Product.Search" resultType="MyApp.Models.Product">
    SELECT
      Id, ProductCode, ProductName, Description, Price,
      StockQuantity, Category, CreatedDate, UpdatedDate, IsActive
    FROM Products
    WHERE IsActive = 1
      AND (ProductName LIKE @Keyword OR ProductCode LIKE @Keyword)
    ORDER BY ProductName
  </select>

  <insert id="Product.Insert">
    INSERT INTO Products (
      ProductCode, ProductName, Description, Price,
      StockQuantity, Category, CreatedDate, IsActive
    )
    VALUES (
      @ProductCode, @ProductName, @Description, @Price,
      @StockQuantity, @Category, @CreatedDate, @IsActive
    )
  </insert>

  <update id="Product.Update">
    UPDATE Products
    SET
      ProductCode = @ProductCode,
      ProductName = @ProductName,
      Description = @Description,
      Price = @Price,
      StockQuantity = @StockQuantity,
      Category = @Category,
      UpdatedDate = @UpdatedDate
    WHERE Id = @Id
  </update>

  <delete id="Product.Delete">
    DELETE FROM Products WHERE Id = @Id
  </delete>

</sqlMap>
```

### Bước 2: Đăng ký SQL Map trong Program.cs

```csharp
builder.Services.AddWscDataAccess(connectionString, options =>
{
    // Đăng ký SQL map files
    options.AddSqlMapFile("SqlMaps/ProductMap.xml");
    options.AddSqlMapFile("SqlMaps/OrderMap.xml");
    // ... thêm các map files khác
});
```

### Bước 3: Tạo Repository Class

```csharp
using WSC.DataAccess.Core;
using WSC.DataAccess.Mapping;
using WSC.DataAccess.Repository;

public class ProductRepository : SqlMapRepository<Product>
{
    public ProductRepository(IDbSessionFactory sessionFactory, SqlMapper sqlMapper)
        : base(sessionFactory, sqlMapper)
    {
    }

    // Query trả về danh sách
    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await QueryListAsync("Product.GetAll");
    }

    // Query trả về single object
    public async Task<Product?> GetByIdAsync(int id)
    {
        return await QuerySingleAsync("Product.GetById", new { Id = id });
    }

    // Query với parameters phức tạp
    public async Task<IEnumerable<Product>> SearchAsync(string keyword)
    {
        return await QueryListAsync("Product.Search", new { Keyword = $"%{keyword}%" });
    }

    // Execute INSERT/UPDATE/DELETE
    public async Task<int> InsertAsync(Product product)
    {
        return await ExecuteAsync("Product.Insert", product);
    }

    public async Task<int> UpdateAsync(Product product)
    {
        product.UpdatedDate = DateTime.Now;
        return await ExecuteAsync("Product.Update", product);
    }

    public async Task<int> DeleteAsync(int id)
    {
        return await ExecuteAsync("Product.Delete", new { Id = id });
    }
}
```

## Các loại Statement

### 1. SELECT Statement

#### Simple SELECT

```xml
<select id="User.GetAll" resultType="MyApp.Models.User">
  SELECT * FROM Users WHERE IsActive = 1
</select>
```

```csharp
public async Task<IEnumerable<User>> GetAllUsersAsync()
{
    return await QueryListAsync("User.GetAll");
}
```

#### SELECT với Parameters

```xml
<select id="User.GetByEmail" resultType="MyApp.Models.User">
  SELECT * FROM Users
  WHERE Email = @Email AND IsActive = 1
</select>
```

```csharp
public async Task<User?> GetByEmailAsync(string email)
{
    return await QuerySingleAsync("User.GetByEmail", new { Email = email });
}
```

#### SELECT với Multiple Parameters

```xml
<select id="Product.SearchAdvanced" resultType="MyApp.Models.Product">
  SELECT * FROM Products
  WHERE (@Category IS NULL OR Category = @Category)
    AND (@MinPrice IS NULL OR Price >= @MinPrice)
    AND (@MaxPrice IS NULL OR Price &lt;= @MaxPrice)
    AND IsActive = 1
  ORDER BY ProductName
</select>
```

```csharp
public async Task<IEnumerable<Product>> SearchAdvancedAsync(
    string? category, decimal? minPrice, decimal? maxPrice)
{
    return await QueryListAsync("Product.SearchAdvanced", new
    {
        Category = category,
        MinPrice = minPrice,
        MaxPrice = maxPrice
    });
}
```

#### SELECT với JOINs

```xml
<select id="Order.GetOrderDetails" resultType="MyApp.DTOs.OrderDetailDto">
  SELECT
    o.Id AS OrderId,
    o.OrderDate,
    o.TotalAmount,
    u.Username,
    u.Email,
    oi.ProductId,
    p.ProductName,
    oi.Quantity,
    oi.UnitPrice
  FROM Orders o
  INNER JOIN Users u ON o.UserId = u.Id
  INNER JOIN OrderItems oi ON o.Id = oi.OrderId
  INNER JOIN Products p ON oi.ProductId = p.Id
  WHERE o.Id = @OrderId
</select>
```

### 2. INSERT Statement

#### Simple INSERT

```xml
<insert id="User.Insert">
  INSERT INTO Users (Username, Email, FullName, CreatedDate, IsActive)
  VALUES (@Username, @Email, @FullName, @CreatedDate, @IsActive)
</insert>
```

```csharp
public async Task<int> CreateUserAsync(User user)
{
    user.CreatedDate = DateTime.Now;
    user.IsActive = true;
    return await ExecuteAsync("User.Insert", user);
}
```

#### INSERT với SCOPE_IDENTITY

```xml
<insert id="User.InsertWithId">
  INSERT INTO Users (Username, Email, FullName, CreatedDate, IsActive)
  VALUES (@Username, @Email, @FullName, @CreatedDate, @IsActive);
  SELECT CAST(SCOPE_IDENTITY() as int)
</insert>
```

```csharp
public async Task<int> CreateUserAsync(User user)
{
    user.CreatedDate = DateTime.Now;
    using var session = SessionFactory.OpenSession();
    return await SqlMapper.ExecuteScalarAsync<int>(
        session, "User.InsertWithId", user);
}
```

### 3. UPDATE Statement

```xml
<update id="Product.UpdatePrice">
  UPDATE Products
  SET Price = @NewPrice,
      UpdatedDate = GETDATE()
  WHERE Id = @ProductId
</update>
```

```csharp
public async Task<int> UpdatePriceAsync(int productId, decimal newPrice)
{
    return await ExecuteAsync("Product.UpdatePrice", new
    {
        ProductId = productId,
        NewPrice = newPrice
    });
}
```

### 4. DELETE Statement

```xml
<!-- Hard delete -->
<delete id="Product.HardDelete">
  DELETE FROM Products WHERE Id = @Id
</delete>

<!-- Soft delete -->
<update id="Product.SoftDelete">
  UPDATE Products
  SET IsActive = 0, UpdatedDate = GETDATE()
  WHERE Id = @Id
</update>
```

### 5. Stored Procedure

```xml
<procedure id="Report.GetMonthlySales" resultType="MyApp.DTOs.MonthlySalesDto">
  usp_GetMonthlySales
</procedure>
```

```csharp
public async Task<IEnumerable<MonthlySalesDto>> GetMonthlySalesAsync(
    int year, int month)
{
    using var session = SessionFactory.OpenSession();
    return await SqlMapper.ExecuteProcedureAsync<MonthlySalesDto>(
        session,
        "Report.GetMonthlySales",
        new { Year = year, Month = month });
}
```

## Ví dụ Thực tế

### Ví dụ 1: Order Management System

**OrderMap.xml**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<sqlMap namespace="Order">

  <!-- Get order with details -->
  <select id="Order.GetWithDetails" resultType="MyApp.Models.Order">
    SELECT
      o.*,
      u.Username,
      u.Email
    FROM Orders o
    INNER JOIN Users u ON o.UserId = u.Id
    WHERE o.Id = @OrderId
  </select>

  <!-- Get order items -->
  <select id="Order.GetItems" resultType="MyApp.Models.OrderItem">
    SELECT
      oi.*,
      p.ProductName,
      p.ProductCode
    FROM OrderItems oi
    INNER JOIN Products p ON oi.ProductId = p.Id
    WHERE oi.OrderId = @OrderId
  </select>

  <!-- Insert order -->
  <insert id="Order.Insert">
    INSERT INTO Orders (UserId, OrderDate, TotalAmount, Status)
    VALUES (@UserId, @OrderDate, @TotalAmount, @Status);
    SELECT CAST(SCOPE_IDENTITY() as int)
  </insert>

  <!-- Insert order item -->
  <insert id="Order.InsertItem">
    INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice)
    VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice)
  </insert>

  <!-- Update order status -->
  <update id="Order.UpdateStatus">
    UPDATE Orders
    SET Status = @Status, UpdatedDate = GETDATE()
    WHERE Id = @OrderId
  </update>

</sqlMap>
```

**OrderRepository.cs**

```csharp
public class OrderRepository : SqlMapRepository<Order>
{
    public OrderRepository(IDbSessionFactory sessionFactory, SqlMapper sqlMapper)
        : base(sessionFactory, sqlMapper)
    {
    }

    public async Task<Order?> GetOrderWithDetailsAsync(int orderId)
    {
        return await QuerySingleAsync("Order.GetWithDetails", new { OrderId = orderId });
    }

    public async Task<IEnumerable<OrderItem>> GetOrderItemsAsync(int orderId)
    {
        return await QueryListAsync("Order.GetItems", new { OrderId = orderId });
    }

    public async Task<int> CreateOrderAsync(Order order, List<OrderItem> items)
    {
        return await ExecuteInTransactionAsync(async (session) =>
        {
            // Insert order
            var orderId = await SqlMapper.ExecuteScalarAsync<int>(
                session, "Order.Insert", order);

            // Insert order items
            foreach (var item in items)
            {
                item.OrderId = orderId;
                await SqlMapper.ExecuteAsync(session, "Order.InsertItem", item);
            }

            return orderId;
        });
    }

    public async Task<int> UpdateOrderStatusAsync(int orderId, string status)
    {
        return await ExecuteAsync("Order.UpdateStatus", new
        {
            OrderId = orderId,
            Status = status
        });
    }
}
```

### Ví dụ 2: Reporting System

**ReportMap.xml**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<sqlMap namespace="Report">

  <select id="Report.SalesByCategory" resultType="MyApp.DTOs.SalesReportDto">
    SELECT
      p.Category,
      COUNT(DISTINCT o.Id) AS TotalOrders,
      SUM(oi.Quantity) AS TotalQuantity,
      SUM(oi.Quantity * oi.UnitPrice) AS TotalRevenue
    FROM Orders o
    INNER JOIN OrderItems oi ON o.Id = oi.OrderId
    INNER JOIN Products p ON oi.ProductId = p.Id
    WHERE o.OrderDate BETWEEN @StartDate AND @EndDate
    GROUP BY p.Category
    ORDER BY TotalRevenue DESC
  </select>

  <select id="Report.TopCustomers" resultType="MyApp.DTOs.CustomerReportDto">
    SELECT TOP (@TopN)
      u.Id,
      u.Username,
      u.Email,
      COUNT(o.Id) AS OrderCount,
      SUM(o.TotalAmount) AS TotalSpent
    FROM Users u
    INNER JOIN Orders o ON u.Id = o.UserId
    WHERE o.OrderDate BETWEEN @StartDate AND @EndDate
    GROUP BY u.Id, u.Username, u.Email
    ORDER BY TotalSpent DESC
  </select>

  <procedure id="Report.MonthlyRevenue" resultType="MyApp.DTOs.MonthlyRevenueDto">
    usp_GetMonthlyRevenue
  </procedure>

</sqlMap>
```

## Best Practices

### 1. Naming Convention

```
Entity.Action
```

Ví dụ:
- `Product.GetAll`
- `Product.GetById`
- `Product.Insert`
- `Order.GetWithDetails`
- `Report.SalesByCategory`

### 2. Tổ chức Files

```
SqlMaps/
├── ProductMap.xml      # Queries cho Product entity
├── OrderMap.xml        # Queries cho Order entity
├── UserMap.xml         # Queries cho User entity
└── ReportMap.xml       # Queries cho reports
```

### 3. SQL Formatting

```xml
<!-- ✅ GOOD: Readable và formatted -->
<select id="Product.Search">
  SELECT
    Id,
    ProductCode,
    ProductName,
    Price
  FROM Products
  WHERE ProductName LIKE @Keyword
    AND IsActive = 1
  ORDER BY ProductName
</select>

<!-- ❌ BAD: Khó đọc -->
<select id="Product.Search">
SELECT Id,ProductCode,ProductName,Price FROM Products WHERE ProductName LIKE @Keyword AND IsActive=1 ORDER BY ProductName
</select>
```

### 4. Parameter Naming

```csharp
// ✅ GOOD: Rõ ràng và match với SQL
await QuerySingleAsync("Product.GetById", new { Id = productId });

// ✅ GOOD: Object properties match parameters
await ExecuteAsync("Product.Insert", product);

// ❌ BAD: Parameter names không match
await QuerySingleAsync("Product.GetById", new { ProductId = productId });
```

### 5. Error Handling

```csharp
public async Task<Product?> GetProductAsync(int id)
{
    try
    {
        return await QuerySingleAsync("Product.GetById", new { Id = id });
    }
    catch (InvalidOperationException ex)
    {
        // Statement not found
        _logger.LogError(ex, "SQL statement 'Product.GetById' not found");
        throw;
    }
    catch (Exception ex)
    {
        // SQL execution error
        _logger.LogError(ex, "Error executing Product.GetById for ID {Id}", id);
        throw;
    }
}
```

### 6. Testing SQL Maps

```csharp
[Fact]
public async Task GetById_ShouldReturnProduct_WhenProductExists()
{
    // Arrange
    var repository = _serviceProvider.GetRequiredService<ProductRepository>();

    // Act
    var product = await repository.GetByIdAsync(1);

    // Assert
    Assert.NotNull(product);
    Assert.Equal(1, product.Id);
}
```

## Migration từ IBatis.Net

Nếu bạn đang migrate từ IBatis.Net, WSC.DataAccess hỗ trợ syntax tương tự:

### Differences

| Feature | IBatis.Net | WSC.DataAccess |
|---------|------------|----------------|
| Parameters | `#value#` hoặc `$value$` | `@value` (SQL Server style) |
| Result mapping | `<resultMap>` | Dapper auto-mapping |
| Dynamic SQL | `<dynamic>`, `<isNotNull>`, etc. | SQL CASE/ISNULL |
| Cache | Built-in | Application-level |

---

**Happy coding!** 🚀
