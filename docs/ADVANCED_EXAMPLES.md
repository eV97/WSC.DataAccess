# Advanced Examples - WSC.DataAccess

Các ví dụ nâng cao về sử dụng WSC.DataAccess trong các tình huống phức tạp.

## Mục lục

1. [Bulk Operations](#1-bulk-operations)
2. [Complex Transactions](#2-complex-transactions)
3. [Dynamic Queries](#3-dynamic-queries)
4. [Performance Optimization](#4-performance-optimization)
5. [Multi-Tenant Support](#5-multi-tenant-support)
6. [Audit Logging](#6-audit-logging)

---

## 1. Bulk Operations

### Bulk Insert với TransactionHelper

```csharp
using WSC.DataAccess.Utilities;

public class ProductService
{
    private readonly IDbSessionFactory _sessionFactory;

    public async Task ImportProductsAsync(List<Product> products)
    {
        // Insert 1000 products trong một transaction
        await BulkOperations.BatchInsertAsync(
            _sessionFactory,
            @"INSERT INTO Products (ProductCode, ProductName, Price, StockQuantity, CreatedDate, IsActive)
              VALUES (@ProductCode, @ProductName, @Price, @StockQuantity, @CreatedDate, @IsActive)",
            products,
            batchSize: 1000);
    }
}
```

### Bulk Update

```csharp
public async Task UpdatePricesAsync(Dictionary<int, decimal> productPrices)
{
    var updateSql = @"
        UPDATE Products
        SET Price = @Price, UpdatedDate = GETDATE()
        WHERE Id = @Id";

    using var session = _sessionFactory.OpenSession();
    session.BeginTransaction();

    try
    {
        foreach (var (productId, price) in productPrices)
        {
            await session.Connection.ExecuteAsync(
                updateSql,
                new { Id = productId, Price = price },
                session.Transaction);
        }

        session.Commit();
    }
    catch
    {
        session.Rollback();
        throw;
    }
}
```

### Upsert (Merge) Operation

```csharp
public async Task UpsertProductAsync(Product product)
{
    var mergeSql = @"
        MERGE INTO Products AS target
        USING (SELECT @ProductCode AS ProductCode) AS source
        ON target.ProductCode = source.ProductCode
        WHEN MATCHED THEN
            UPDATE SET
                ProductName = @ProductName,
                Price = @Price,
                StockQuantity = @StockQuantity,
                UpdatedDate = GETDATE()
        WHEN NOT MATCHED THEN
            INSERT (ProductCode, ProductName, Price, StockQuantity, CreatedDate, IsActive)
            VALUES (@ProductCode, @ProductName, @Price, @StockQuantity, GETDATE(), 1);";

    await BulkOperations.UpsertAsync(_sessionFactory, mergeSql, product);
}
```

---

## 2. Complex Transactions

### Multi-Table Transaction

```csharp
public class OrderService
{
    private readonly IDbSessionFactory _sessionFactory;

    public async Task<int> CreateOrderWithItemsAsync(
        Order order,
        List<OrderItem> items)
    {
        return await TransactionHelper.ExecuteInTransactionAsync(
            _sessionFactory,
            async (session) =>
            {
                // 1. Insert order
                var orderSql = @"
                    INSERT INTO Orders (UserId, OrderDate, TotalAmount, Status)
                    VALUES (@UserId, @OrderDate, @TotalAmount, @Status);
                    SELECT CAST(SCOPE_IDENTITY() as int)";

                var orderId = await session.Connection.ExecuteScalarAsync<int>(
                    orderSql, order, session.Transaction);

                // 2. Insert order items
                foreach (var item in items)
                {
                    item.OrderId = orderId;

                    var itemSql = @"
                        INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice)
                        VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice)";

                    await session.Connection.ExecuteAsync(
                        itemSql, item, session.Transaction);
                }

                // 3. Update inventory
                foreach (var item in items)
                {
                    var updateInventorySql = @"
                        UPDATE Products
                        SET StockQuantity = StockQuantity - @Quantity
                        WHERE Id = @ProductId AND StockQuantity >= @Quantity";

                    var affected = await session.Connection.ExecuteAsync(
                        updateInventorySql,
                        new { item.ProductId, item.Quantity },
                        session.Transaction);

                    if (affected == 0)
                    {
                        throw new InvalidOperationException(
                            $"Insufficient stock for product {item.ProductId}");
                    }
                }

                return orderId;
            },
            IsolationLevel.Serializable); // Prevent concurrency issues
    }
}
```

### Nested Transactions (Savepoints)

```csharp
public async Task ProcessOrderWithSavepointAsync(Order order)
{
    using var session = _sessionFactory.OpenSession();
    session.BeginTransaction();

    try
    {
        // Insert order
        var orderId = await InsertOrderAsync(session, order);

        // Create savepoint
        var command = session.CreateCommand();
        command.CommandText = "SAVE TRANSACTION OrderItemsSavepoint";
        await command.ExecuteNonQueryAsync();

        try
        {
            // Insert order items (might fail)
            await InsertOrderItemsAsync(session, orderId, order.Items);
        }
        catch
        {
            // Rollback to savepoint
            command.CommandText = "ROLLBACK TRANSACTION OrderItemsSavepoint";
            await command.ExecuteNonQueryAsync();

            // Continue with default items
            await InsertDefaultItemsAsync(session, orderId);
        }

        session.Commit();
    }
    catch
    {
        session.Rollback();
        throw;
    }
}
```

### Transaction with Retry on Deadlock

```csharp
public async Task<int> UpdateStockWithRetryAsync(int productId, int quantity)
{
    return await TransactionHelper.ExecuteWithRetryAsync(
        _sessionFactory,
        async (session) =>
        {
            var sql = @"
                UPDATE Products
                SET StockQuantity = StockQuantity - @Quantity
                WHERE Id = @ProductId";

            return await session.Connection.ExecuteAsync(
                sql,
                new { ProductId = productId, Quantity = quantity },
                session.Transaction);
        },
        maxRetries: 3,
        delayMilliseconds: 100);
}
```

---

## 3. Dynamic Queries

### Dynamic WHERE Clause

```csharp
public class ProductRepository : BaseRepository<Product>
{
    public async Task<IEnumerable<Product>> SearchProductsAsync(ProductSearchCriteria criteria)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        // Build dynamic WHERE clause
        if (!string.IsNullOrEmpty(criteria.ProductName))
        {
            conditions.Add("ProductName LIKE @ProductName");
            parameters.Add("ProductName", $"%{criteria.ProductName}%");
        }

        if (criteria.MinPrice.HasValue)
        {
            conditions.Add("Price >= @MinPrice");
            parameters.Add("MinPrice", criteria.MinPrice.Value);
        }

        if (criteria.MaxPrice.HasValue)
        {
            conditions.Add("Price <= @MaxPrice");
            parameters.Add("MaxPrice", criteria.MaxPrice.Value);
        }

        if (!string.IsNullOrEmpty(criteria.Category))
        {
            conditions.Add("Category = @Category");
            parameters.Add("Category", criteria.Category);
        }

        conditions.Add("IsActive = 1");

        var whereClause = string.Join(" AND ", conditions);
        var sql = $"SELECT * FROM Products WHERE {whereClause} ORDER BY ProductName";

        using var session = SessionFactory.OpenSession();
        return await session.Connection.QueryAsync<Product>(sql, parameters);
    }
}

public class ProductSearchCriteria
{
    public string? ProductName { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Category { get; set; }
}
```

### Dynamic Sorting

```csharp
public async Task<IEnumerable<Product>> GetProductsAsync(
    string? sortBy = "ProductName",
    string? sortDirection = "ASC")
{
    // Validate sort column (prevent SQL injection)
    var validColumns = new[] { "ProductName", "Price", "StockQuantity", "CreatedDate" };
    if (!validColumns.Contains(sortBy))
        sortBy = "ProductName";

    // Validate sort direction
    sortDirection = sortDirection?.ToUpper() == "DESC" ? "DESC" : "ASC";

    var sql = $@"
        SELECT * FROM Products
        WHERE IsActive = 1
        ORDER BY {sortBy} {sortDirection}";

    using var session = SessionFactory.OpenSession();
    return await session.Connection.QueryAsync<Product>(sql);
}
```

### Pagination

```csharp
public async Task<PagedResult<Product>> GetProductsPagedAsync(
    int page = 1,
    int pageSize = 20,
    string? category = null)
{
    var offset = (page - 1) * pageSize;

    var conditions = new List<string> { "IsActive = 1" };
    var parameters = new DynamicParameters();
    parameters.Add("Offset", offset);
    parameters.Add("PageSize", pageSize);

    if (!string.IsNullOrEmpty(category))
    {
        conditions.Add("Category = @Category");
        parameters.Add("Category", category);
    }

    var whereClause = string.Join(" AND ", conditions);

    // Get total count
    var countSql = $"SELECT COUNT(*) FROM Products WHERE {whereClause}";
    var dataSql = $@"
        SELECT * FROM Products
        WHERE {whereClause}
        ORDER BY ProductName
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY";

    using var session = SessionFactory.OpenSession();

    var totalCount = await session.Connection.ExecuteScalarAsync<int>(countSql, parameters);
    var data = await session.Connection.QueryAsync<Product>(dataSql, parameters);

    return new PagedResult<Product>
    {
        Data = data.ToList(),
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
    };
}

public class PagedResult<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
```

---

## 4. Performance Optimization

### Caching Strategy

```csharp
using Microsoft.Extensions.Caching.Memory;

public class CachedProductRepository : ProductRepository
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public CachedProductRepository(
        IDbSessionFactory sessionFactory,
        SqlMapper sqlMapper,
        IMemoryCache cache)
        : base(sessionFactory, sqlMapper)
    {
        _cache = cache;
    }

    public async Task<Product?> GetByIdCachedAsync(int id)
    {
        var cacheKey = $"Product_{id}";

        if (_cache.TryGetValue(cacheKey, out Product? product))
        {
            return product;
        }

        product = await GetByIdAsync(id);

        if (product != null)
        {
            _cache.Set(cacheKey, product, CacheDuration);
        }

        return product;
    }

    public override async Task<int> UpdateAsync(Product product)
    {
        var result = await base.UpdateAsync(product);

        // Invalidate cache
        _cache.Remove($"Product_{product.Id}");

        return result;
    }
}
```

### Connection Multiplexing

```csharp
public class RepositoryFactory
{
    private readonly IDbSessionFactory _sessionFactory;

    // Reuse sessions across multiple repositories in a single request
    public async Task ProcessOrderAsync(Order order)
    {
        using var session = _sessionFactory.OpenSession();
        session.BeginTransaction();

        try
        {
            // All repositories share the same session and transaction
            var orderRepo = new OrderRepository(session);
            var productRepo = new ProductRepository(session);
            var inventoryRepo = new InventoryRepository(session);

            await orderRepo.InsertAsync(order);
            await productRepo.UpdateStockAsync(order.ProductId, order.Quantity);
            await inventoryRepo.RecordMovementAsync(order);

            session.Commit();
        }
        catch
        {
            session.Rollback();
            throw;
        }
    }
}
```

### Query Result Caching

```csharp
public class QueryCache<T>
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _defaultExpiration;

    public QueryCache(IMemoryCache cache, TimeSpan? defaultExpiration = null)
    {
        _cache = cache;
        _defaultExpiration = defaultExpiration ?? TimeSpan.FromMinutes(5);
    }

    public async Task<T> GetOrCreateAsync(
        string cacheKey,
        Func<Task<T>> factory,
        TimeSpan? expiration = null)
    {
        if (_cache.TryGetValue(cacheKey, out T? value))
        {
            return value!;
        }

        value = await factory();

        _cache.Set(cacheKey, value, expiration ?? _defaultExpiration);

        return value;
    }
}

// Usage
public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category)
{
    var cacheKey = $"Products_Category_{category}";

    return await _queryCache.GetOrCreateAsync(
        cacheKey,
        async () => await QueryListAsync("Product.GetByCategory", new { Category = category }),
        TimeSpan.FromMinutes(10));
}
```

---

## 5. Multi-Tenant Support

### Tenant-Based Connection

```csharp
public class TenantDbSessionFactory : IDbSessionFactory
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ITenantProvider _tenantProvider;
    private readonly Dictionary<string, string> _tenantConnections;

    public TenantDbSessionFactory(
        IDbConnectionFactory connectionFactory,
        ITenantProvider tenantProvider,
        Dictionary<string, string> tenantConnections)
    {
        _connectionFactory = connectionFactory;
        _tenantProvider = tenantProvider;
        _tenantConnections = tenantConnections;
    }

    public DbSession OpenSession()
    {
        var tenantId = _tenantProvider.GetCurrentTenant();

        if (!_tenantConnections.TryGetValue(tenantId, out var connectionString))
        {
            throw new InvalidOperationException($"Connection string for tenant '{tenantId}' not found");
        }

        var connection = _connectionFactory.CreateConnection(connectionString);
        return new DbSession(connection);
    }

    public DbSession OpenSession(string connectionName)
    {
        var connection = _connectionFactory.CreateConnection(_tenantConnections[connectionName]);
        return new DbSession(connection);
    }
}

public interface ITenantProvider
{
    string GetCurrentTenant();
}

public class HttpContextTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetCurrentTenant()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-ID"].ToString()
            ?? throw new InvalidOperationException("Tenant ID not found in request headers");
    }
}
```

### Tenant-Filtered Queries

```csharp
public class TenantAwareRepository<T> : BaseRepository<T> where T : class, ITenantEntity
{
    private readonly ITenantProvider _tenantProvider;

    public TenantAwareRepository(
        IDbSessionFactory sessionFactory,
        ITenantProvider tenantProvider)
        : base(sessionFactory)
    {
        _tenantProvider = tenantProvider;
    }

    public override async Task<IEnumerable<T>> GetAllAsync()
    {
        var tenantId = _tenantProvider.GetCurrentTenant();
        var sql = $"SELECT * FROM {TableName} WHERE TenantId = @TenantId";

        using var session = SessionFactory.OpenSession();
        return await session.Connection.QueryAsync<T>(sql, new { TenantId = tenantId });
    }

    public override async Task<T?> GetByIdAsync(object id)
    {
        var tenantId = _tenantProvider.GetCurrentTenant();
        var sql = $"SELECT * FROM {TableName} WHERE {KeyColumn} = @Id AND TenantId = @TenantId";

        using var session = SessionFactory.OpenSession();
        return await session.Connection.QueryFirstOrDefaultAsync<T>(
            sql, new { Id = id, TenantId = tenantId });
    }
}

public interface ITenantEntity
{
    string TenantId { get; set; }
}
```

---

## 6. Audit Logging

### Automatic Audit Trail

```csharp
public class AuditableRepository<T> : BaseRepository<T> where T : class, IAuditable
{
    private readonly string _currentUser;

    public AuditableRepository(
        IDbSessionFactory sessionFactory,
        IHttpContextAccessor httpContextAccessor)
        : base(sessionFactory)
    {
        _currentUser = httpContextAccessor.HttpContext?.User.Identity?.Name ?? "System";
    }

    public override async Task<int> InsertAsync(T entity)
    {
        entity.CreatedBy = _currentUser;
        entity.CreatedDate = DateTime.UtcNow;
        entity.ModifiedBy = _currentUser;
        entity.ModifiedDate = DateTime.UtcNow;

        return await base.InsertAsync(entity);
    }

    public override async Task<int> UpdateAsync(T entity)
    {
        entity.ModifiedBy = _currentUser;
        entity.ModifiedDate = DateTime.UtcNow;

        return await base.UpdateAsync(entity);
    }
}

public interface IAuditable
{
    string CreatedBy { get; set; }
    DateTime CreatedDate { get; set; }
    string ModifiedBy { get; set; }
    DateTime ModifiedDate { get; set; }
}
```

### Change Tracking

```csharp
public class AuditLogService
{
    private readonly IDbSessionFactory _sessionFactory;

    public async Task LogChangeAsync<T>(
        string entityType,
        object entityId,
        T oldValue,
        T newValue,
        string userId)
    {
        var changes = CompareObjects(oldValue, newValue);

        var auditLog = new AuditLog
        {
            EntityType = entityType,
            EntityId = entityId.ToString()!,
            UserId = userId,
            Action = "Update",
            Changes = System.Text.Json.JsonSerializer.Serialize(changes),
            Timestamp = DateTime.UtcNow
        };

        var sql = @"
            INSERT INTO AuditLogs (EntityType, EntityId, UserId, Action, Changes, Timestamp)
            VALUES (@EntityType, @EntityId, @UserId, @Action, @Changes, @Timestamp)";

        using var session = _sessionFactory.OpenSession();
        await session.Connection.ExecuteAsync(sql, auditLog);
    }

    private Dictionary<string, (object? OldValue, object? NewValue)> CompareObjects<T>(
        T oldObj,
        T newObj)
    {
        var changes = new Dictionary<string, (object?, object?)>();
        var properties = typeof(T).GetProperties();

        foreach (var prop in properties)
        {
            var oldValue = prop.GetValue(oldObj);
            var newValue = prop.GetValue(newObj);

            if (!Equals(oldValue, newValue))
            {
                changes[prop.Name] = (oldValue, newValue);
            }
        }

        return changes;
    }
}
```

---

**Chúc bạn áp dụng thành công!** 🚀
