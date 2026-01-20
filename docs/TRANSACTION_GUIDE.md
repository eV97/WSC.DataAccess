# Transaction Management Guide

Hướng dẫn đầy đủ về quản lý Transaction trong WSC.DataAccess.

## 📋 Tổng quan

WSC.DataAccess hỗ trợ **FULL TRANSACTION MANAGEMENT** với:
- ✅ Auto Rollback khi có lỗi
- ✅ Multi-table operations
- ✅ Nested operations
- ✅ Custom isolation levels
- ✅ Retry logic cho deadlocks

## 🎯 Use Case: Lưu vào Nhiều Bảng

### Scenario

Bạn muốn:
1. Insert vào bảng Order
2. Insert vào bảng OrderItems (nhiều items)
3. Update bảng Products (trừ inventory)
4. Insert vào bảng AuditLog

**Nếu BẤT KỲ operation nào FAIL** → **ROLLBACK TẤT CẢ**!

---

## 📝 Cách 1: Manual Transaction (Recommended)

### Ưu điểm
- Full control
- Dễ debug
- Clear và explicit

### Code Example

```csharp
public class OrderService
{
    private readonly IDbSessionFactory _sessionFactory;
    private readonly SqlMapper _sqlMapper;

    public async Task<int> CreateOrderWithItemsAsync(
        Order order,
        List<OrderItem> items)
    {
        // 1. Mở session
        using var session = _sessionFactory.OpenSession();

        // 2. Bắt đầu transaction
        session.BeginTransaction();

        try
        {
            // 3. INSERT vào bảng Orders
            var orderSql = @"
                INSERT INTO Orders (CustomerId, OrderDate, TotalAmount, Status)
                VALUES (@CustomerId, @OrderDate, @TotalAmount, @Status);
                SELECT CAST(SCOPE_IDENTITY() as int)";

            var orderId = await session.Connection.ExecuteScalarAsync<int>(
                orderSql,
                new
                {
                    order.CustomerId,
                    order.OrderDate,
                    order.TotalAmount,
                    Status = "Pending"
                },
                session.Transaction);

            Console.WriteLine($"✓ Order created: {orderId}");

            // 4. INSERT vào bảng OrderItems (nhiều records)
            foreach (var item in items)
            {
                var itemSql = @"
                    INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice)
                    VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice)";

                await session.Connection.ExecuteAsync(
                    itemSql,
                    new
                    {
                        OrderId = orderId,
                        item.ProductId,
                        item.Quantity,
                        item.UnitPrice
                    },
                    session.Transaction);

                Console.WriteLine($"  ✓ Item added: Product {item.ProductId} x {item.Quantity}");
            }

            // 5. UPDATE bảng Products (trừ inventory)
            foreach (var item in items)
            {
                var updateSql = @"
                    UPDATE Products
                    SET StockQuantity = StockQuantity - @Quantity,
                        UpdatedDate = GETDATE()
                    WHERE Id = @ProductId
                      AND StockQuantity >= @Quantity";

                var affected = await session.Connection.ExecuteAsync(
                    updateSql,
                    new { item.ProductId, item.Quantity },
                    session.Transaction);

                // ❌ KIỂM TRA: Nếu không đủ stock → THROW ERROR → AUTO ROLLBACK
                if (affected == 0)
                {
                    throw new InvalidOperationException(
                        $"Insufficient stock for Product {item.ProductId}");
                }

                Console.WriteLine($"  ✓ Stock updated: Product {item.ProductId}");
            }

            // 6. INSERT vào bảng AuditLog
            var auditSql = @"
                INSERT INTO AuditLog (Action, EntityType, EntityId, UserId, Timestamp)
                VALUES (@Action, @EntityType, @EntityId, @UserId, GETDATE())";

            await session.Connection.ExecuteAsync(
                auditSql,
                new
                {
                    Action = "CREATE_ORDER",
                    EntityType = "Order",
                    EntityId = orderId,
                    UserId = order.CustomerId
                },
                session.Transaction);

            Console.WriteLine("✓ Audit log created");

            // 7. COMMIT - Tất cả thành công
            session.Commit();
            Console.WriteLine("✅ TRANSACTION COMMITTED - All operations successful!");

            return orderId;
        }
        catch (Exception ex)
        {
            // 8. ROLLBACK - Có lỗi xảy ra
            session.Rollback();
            Console.WriteLine($"❌ TRANSACTION ROLLED BACK - Error: {ex.Message}");
            throw;
        }
    }
}
```

### Giải thích Flow

```
BEGIN TRANSACTION
  ├─ INSERT Orders          ✅ Success → Continue
  ├─ INSERT OrderItems (1)  ✅ Success → Continue
  ├─ INSERT OrderItems (2)  ✅ Success → Continue
  ├─ UPDATE Products (1)    ✅ Success → Continue
  ├─ UPDATE Products (2)    ❌ FAIL (Không đủ stock)
  └─ ROLLBACK
      └─ Tất cả operations bị hủy!
          - Order bị xóa
          - OrderItems bị xóa
          - Products không bị update
          - AuditLog không tạo
```

---

## 📝 Cách 2: Sử dụng SqlMaps (IBatis-style)

### Code Example với XML Maps

**OrderMap.xml**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<sqlMap namespace="Order">

  <insert id="Order.Insert">
    <![CDATA[
      INSERT INTO Orders (CustomerId, OrderDate, TotalAmount, Status)
      VALUES (@CustomerId, @OrderDate, @TotalAmount, @Status);
      SELECT CAST(SCOPE_IDENTITY() as int)
    ]]>
  </insert>

  <insert id="Order.InsertItem">
    INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice)
    VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice)
  </insert>

  <update id="Order.UpdateProductStock">
    <![CDATA[
      UPDATE Products
      SET StockQuantity = StockQuantity - @Quantity,
          UpdatedDate = GETDATE()
      WHERE Id = @ProductId
        AND StockQuantity >= @Quantity
    ]]>
  </update>

  <insert id="Order.InsertAuditLog">
    INSERT INTO AuditLog (Action, EntityType, EntityId, UserId, Timestamp)
    VALUES (@Action, @EntityType, @EntityId, @UserId, GETDATE())
  </insert>

</sqlMap>
```

**Repository Code**

```csharp
public class OrderRepository : SqlMapRepository<Order>
{
    public OrderRepository(
        IDbSessionFactory sessionFactory,
        SqlMapper sqlMapper)
        : base(sessionFactory, sqlMapper)
    {
    }

    public async Task<int> CreateOrderWithItemsAsync(
        Order order,
        List<OrderItem> items)
    {
        using var session = SessionFactory.OpenSession();
        session.BeginTransaction();

        try
        {
            // 1. Insert Order
            var orderId = await SqlMapper.ExecuteScalarAsync<int>(
                session,
                "Order.Insert",
                new
                {
                    order.CustomerId,
                    order.OrderDate,
                    order.TotalAmount,
                    Status = "Pending"
                });

            // 2. Insert OrderItems
            foreach (var item in items)
            {
                await SqlMapper.ExecuteAsync(
                    session,
                    "Order.InsertItem",
                    new
                    {
                        OrderId = orderId,
                        item.ProductId,
                        item.Quantity,
                        item.UnitPrice
                    });
            }

            // 3. Update Products Stock
            foreach (var item in items)
            {
                var affected = await SqlMapper.ExecuteAsync(
                    session,
                    "Order.UpdateProductStock",
                    new { item.ProductId, item.Quantity });

                if (affected == 0)
                {
                    throw new InvalidOperationException(
                        $"Insufficient stock for Product {item.ProductId}");
                }
            }

            // 4. Insert Audit Log
            await SqlMapper.ExecuteAsync(
                session,
                "Order.InsertAuditLog",
                new
                {
                    Action = "CREATE_ORDER",
                    EntityType = "Order",
                    EntityId = orderId,
                    UserId = order.CustomerId
                });

            // Commit all
            session.Commit();
            return orderId;
        }
        catch
        {
            session.Rollback();
            throw;
        }
    }
}
```

---

## 📝 Cách 3: TransactionHelper (Simplified)

### Sử dụng TransactionHelper Utility

```csharp
using WSC.DataAccess.Utilities;

public class OrderService
{
    private readonly IDbSessionFactory _sessionFactory;

    public async Task<int> CreateOrderAsync(Order order, List<OrderItem> items)
    {
        // TransactionHelper tự động handle BEGIN, COMMIT, ROLLBACK
        return await TransactionHelper.ExecuteInTransactionAsync(
            _sessionFactory,
            async (session) =>
            {
                // Insert Order
                var orderId = await session.Connection.ExecuteScalarAsync<int>(
                    "INSERT INTO Orders (...) VALUES (...); SELECT SCOPE_IDENTITY()",
                    order,
                    session.Transaction);

                // Insert OrderItems
                foreach (var item in items)
                {
                    await session.Connection.ExecuteAsync(
                        "INSERT INTO OrderItems (...) VALUES (...)",
                        new { OrderId = orderId, item.ProductId, item.Quantity },
                        session.Transaction);
                }

                // Update Products
                foreach (var item in items)
                {
                    var affected = await session.Connection.ExecuteAsync(
                        "UPDATE Products SET StockQuantity = StockQuantity - @Quantity WHERE Id = @ProductId AND StockQuantity >= @Quantity",
                        new { item.ProductId, item.Quantity },
                        session.Transaction);

                    if (affected == 0)
                        throw new InvalidOperationException("Insufficient stock");
                }

                return orderId;
            },
            IsolationLevel.Serializable); // Prevent concurrency issues
    }
}
```

---

## 🔒 Isolation Levels

### Chọn Isolation Level phù hợp

```csharp
// Read Committed (Default) - Đủ cho hầu hết cases
session.BeginTransaction(IsolationLevel.ReadCommitted);

// Serializable - Chặn concurrency issues (ví dụ: inventory updates)
session.BeginTransaction(IsolationLevel.Serializable);

// Read Uncommitted - Fastest nhưng có thể đọc dirty data
session.BeginTransaction(IsolationLevel.ReadUncommitted);

// Repeatable Read - Prevent non-repeatable reads
session.BeginTransaction(IsolationLevel.RepeatableRead);
```

### Khi nào dùng Serializable?

```csharp
// ✅ DÙNG Serializable cho inventory/stock updates
public async Task<int> CreateOrderAsync(Order order, List<OrderItem> items)
{
    using var session = _sessionFactory.OpenSession();

    // Serializable = Chặn 2 orders cùng update stock của 1 product
    session.BeginTransaction(IsolationLevel.Serializable);

    try
    {
        // ... operations ...
        session.Commit();
    }
    catch
    {
        session.Rollback();
        throw;
    }
}
```

---

## 🔄 Retry Logic cho Deadlocks

### Automatic Retry

```csharp
using WSC.DataAccess.Utilities;

public async Task<int> CreateOrderWithRetryAsync(Order order, List<OrderItem> items)
{
    // Tự động retry 3 lần nếu gặp deadlock
    return await TransactionHelper.ExecuteWithRetryAsync(
        _sessionFactory,
        async (session) =>
        {
            // Insert Order
            var orderId = await InsertOrderAsync(session, order);

            // Insert Items
            foreach (var item in items)
            {
                await InsertOrderItemAsync(session, orderId, item);
                await UpdateProductStockAsync(session, item);
            }

            return orderId;
        },
        maxRetries: 3,
        delayMilliseconds: 100);
}
```

---

## 🎯 Complete Example - Real World

```csharp
public class OrderService
{
    private readonly IDbSessionFactory _sessionFactory;
    private readonly SqlMapper _sqlMapper;
    private readonly ILogger<OrderService> _logger;

    public async Task<int> CreateOrderAsync(
        int customerId,
        List<OrderItemRequest> items)
    {
        using var session = _sessionFactory.OpenSession();
        session.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            _logger.LogInformation("Creating order for customer {CustomerId}", customerId);

            // Step 1: Validate products exist và có đủ stock
            foreach (var item in items)
            {
                var product = await session.Connection.QueryFirstOrDefaultAsync<Product>(
                    "SELECT * FROM Products WHERE Id = @Id",
                    new { Id = item.ProductId },
                    session.Transaction);

                if (product == null)
                {
                    throw new InvalidOperationException($"Product {item.ProductId} not found");
                }

                if (product.StockQuantity < item.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Insufficient stock for {product.ProductName}. Available: {product.StockQuantity}, Requested: {item.Quantity}");
                }
            }

            // Step 2: Calculate total
            var totalAmount = 0m;
            foreach (var item in items)
            {
                var price = await session.Connection.ExecuteScalarAsync<decimal>(
                    "SELECT Price FROM Products WHERE Id = @Id",
                    new { Id = item.ProductId },
                    session.Transaction);

                totalAmount += price * item.Quantity;
            }

            // Step 3: Insert Order
            var orderSql = @"
                INSERT INTO Orders (CustomerId, OrderDate, TotalAmount, Status, CreatedDate)
                VALUES (@CustomerId, GETDATE(), @TotalAmount, 'Pending', GETDATE());
                SELECT CAST(SCOPE_IDENTITY() as int)";

            var orderId = await session.Connection.ExecuteScalarAsync<int>(
                orderSql,
                new { CustomerId = customerId, TotalAmount = totalAmount },
                session.Transaction);

            _logger.LogInformation("Order {OrderId} created", orderId);

            // Step 4: Insert OrderItems và Update Products
            foreach (var item in items)
            {
                // Insert OrderItem
                await session.Connection.ExecuteAsync(
                    @"INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice)
                      SELECT @OrderId, @ProductId, @Quantity, Price
                      FROM Products WHERE Id = @ProductId",
                    new { OrderId = orderId, item.ProductId, item.Quantity },
                    session.Transaction);

                // Update Product Stock
                var updated = await session.Connection.ExecuteAsync(
                    @"UPDATE Products
                      SET StockQuantity = StockQuantity - @Quantity,
                          UpdatedDate = GETDATE()
                      WHERE Id = @ProductId
                        AND StockQuantity >= @Quantity",
                    new { item.ProductId, item.Quantity },
                    session.Transaction);

                if (updated == 0)
                {
                    throw new InvalidOperationException(
                        $"Concurrency issue: Stock changed for Product {item.ProductId}");
                }

                _logger.LogInformation("Product {ProductId} stock reduced by {Quantity}",
                    item.ProductId, item.Quantity);
            }

            // Step 5: Insert Audit Log
            await session.Connection.ExecuteAsync(
                @"INSERT INTO AuditLog (Action, EntityType, EntityId, UserId, Details, Timestamp)
                  VALUES ('ORDER_CREATED', 'Order', @OrderId, @CustomerId, @Details, GETDATE())",
                new
                {
                    OrderId = orderId,
                    CustomerId = customerId,
                    Details = $"{items.Count} items, Total: {totalAmount:C}"
                },
                session.Transaction);

            // Step 6: Commit
            session.Commit();
            _logger.LogInformation("Order {OrderId} committed successfully", orderId);

            return orderId;
        }
        catch (Exception ex)
        {
            session.Rollback();
            _logger.LogError(ex, "Order creation failed, transaction rolled back");
            throw;
        }
    }
}
```

---

## ⚠️ Common Mistakes

### ❌ Mistake 1: Quên Rollback

```csharp
// ❌ BAD
try
{
    session.BeginTransaction();
    // ... operations ...
    session.Commit();
}
catch (Exception ex)
{
    // QUÊN ROLLBACK!
    throw;
}

// ✅ GOOD
try
{
    session.BeginTransaction();
    // ... operations ...
    session.Commit();
}
catch
{
    session.Rollback();
    throw;
}
```

### ❌ Mistake 2: Không dùng Transaction

```csharp
// ❌ BAD - Không có transaction
using var session = _sessionFactory.OpenSession();
await InsertOrderAsync(session);
await InsertItemsAsync(session);
// Nếu InsertItemsAsync fail → Order vẫn tồn tại!

// ✅ GOOD - Có transaction
using var session = _sessionFactory.OpenSession();
session.BeginTransaction();
try
{
    await InsertOrderAsync(session);
    await InsertItemsAsync(session);
    session.Commit();
}
catch
{
    session.Rollback();
    throw;
}
```

### ❌ Mistake 3: Nested Transactions sai

```csharp
// ❌ BAD - Cannot nest transactions
session.BeginTransaction();
session.BeginTransaction(); // ERROR!

// ✅ GOOD - Reuse same transaction
session.BeginTransaction();
// All operations use session.Transaction
```

---

## 📊 Testing Transactions

```csharp
[Fact]
public async Task CreateOrder_ShouldRollback_WhenStockInsufficient()
{
    // Arrange
    var order = new Order { CustomerId = 1 };
    var items = new List<OrderItem>
    {
        new() { ProductId = 1, Quantity = 1000 } // Stock chỉ có 10
    };

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(async () =>
    {
        await _orderService.CreateOrderAsync(order, items);
    });

    // Verify: Order KHÔNG tồn tại
    var orderCount = await GetOrderCountAsync();
    Assert.Equal(0, orderCount);

    // Verify: Product stock KHÔNG thay đổi
    var product = await GetProductAsync(1);
    Assert.Equal(10, product.StockQuantity);
}
```

---

## 📝 Summary

### Khi nào dùng Transaction?

✅ **LUÔN DÙNG** khi:
- Insert/Update nhiều bảng liên quan
- Update inventory/stock
- Financial operations
- Bất kỳ operation nào cần ALL-OR-NOTHING

### Best Practices

1. ✅ Luôn wrap trong try-catch
2. ✅ Luôn Rollback trong catch
3. ✅ Dùng using để auto-dispose session
4. ✅ Chọn isolation level phù hợp
5. ✅ Log operations cho debugging
6. ✅ Validate trước khi commit
7. ✅ Keep transactions SHORT
8. ✅ Retry on deadlocks

---

**Transaction Management = Data Integrity!** 🔒
