using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Core;
using WSC.DataAccess.Repository;
using WSC.DataAccess.Sample.Models;

namespace WSC.DataAccess.Sample.Services;

/// <summary>
/// Demo service sử dụng NHIỀU DAOs (Multiple Domains)
/// Pattern: MultiDaoProviderRepository với Provider pattern
/// Use case: Service phức tạp cần truy cập nhiều domains (User, Product, Order)
/// </summary>
public class ComplexBusinessService : MultiDaoProviderRepository<dynamic>
{
    // ✅ Khai báo nhiều DAO names từ Provider
    private static readonly string[] DAO_NAMES = new[]
    {
        Provider.DAO001,  // User Management
        Provider.DAO002,  // Product Management
        Provider.DAO003   // Order Management
    };

    public ComplexBusinessService(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider,
        ILogger<ComplexBusinessService> logger)
        : base(sessionFactory, provider, DAO_NAMES, logger: logger)
    {
        // Provider tự động resolve:
        // Provider.DAO001 → "SqlMaps/DAO001.xml"
        // Provider.DAO002 → "SqlMaps/DAO002.xml"
        // Provider.DAO003 → "SqlMaps/DAO003.xml"
        // Tất cả statements từ 3 files này đều available
    }

    #region User Management (từ DAO001.xml)

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        Logger?.LogInformation("Getting all users");
        return await QueryListAsync<User>("User.GetAllUsers");
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        Logger?.LogInformation("Getting user {UserId}", id);
        return await QuerySingleAsync<User>("User.GetUserById", new { Id = id });
    }

    public async Task<int> CreateUserAsync(User user)
    {
        Logger?.LogInformation("Creating user {Email}", user.Email);
        return await ExecuteAsync("User.InsertUser", user);
    }

    #endregion

    #region Product Management (từ DAO002.xml)

    public async Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        Logger?.LogInformation("Getting all products");
        return await QueryListAsync<Product>("Product.GetAllProducts");
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        Logger?.LogInformation("Getting product {ProductId}", id);
        return await QuerySingleAsync<Product>("Product.GetProductById", new { Id = id });
    }

    public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
    {
        Logger?.LogInformation("Getting products by category {CategoryId}", categoryId);
        return await QueryListAsync<Product>("Product.GetProductsByCategory", new { CategoryId = categoryId });
    }

    #endregion

    #region Order Management (từ DAO003.xml)

    public async Task<IEnumerable<Order>> GetAllOrdersAsync()
    {
        Logger?.LogInformation("Getting all orders");
        return await QueryListAsync<Order>("Order.GetAllOrders");
    }

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        Logger?.LogInformation("Getting order {OrderId}", id);
        return await QuerySingleAsync<Order>("Order.GetOrderById", new { Id = id });
    }

    public async Task<IEnumerable<Order>> GetOrdersByUserAsync(int userId)
    {
        Logger?.LogInformation("Getting orders by user {UserId}", userId);
        return await QueryListAsync<Order>("Order.GetOrdersByUser", new { UserId = userId });
    }

    #endregion

    #region Complex Business Logic (Cross-Domain)

    /// <summary>
    /// Business logic phức tạp sử dụng nhiều DAOs trong 1 transaction
    /// Example: Tạo order mới và cập nhật product stock
    /// </summary>
    public async Task<int> CreateOrderWithStockUpdateAsync(Order order, List<OrderItem> items)
    {
        Logger?.LogInformation("Creating order for user {UserId} with {ItemCount} items",
            order.UserId, items.Count);

        return await ExecuteInTransactionAsync(async session =>
        {
            // 1. Insert order (DAO003)
            var orderId = await SqlMapper.ExecuteAsync(session, "Order.InsertOrder", order);

            // 2. Insert order items (DAO003)
            foreach (var item in items)
            {
                item.OrderId = orderId;
                await SqlMapper.ExecuteAsync(session, "Order.InsertOrderItem", item);
            }

            // 3. Update product stock (DAO002)
            foreach (var item in items)
            {
                await SqlMapper.ExecuteAsync(session, "Product.UpdateStock",
                    new { ProductId = item.ProductId, Quantity = -item.Quantity });
            }

            Logger?.LogInformation("Order {OrderId} created successfully with stock updated", orderId);
            return orderId;
        });
    }

    /// <summary>
    /// Get user's order summary with product details
    /// Cross-domain query: User + Order + Product
    /// </summary>
    public async Task<UserOrderSummary> GetUserOrderSummaryAsync(int userId)
    {
        Logger?.LogInformation("Getting order summary for user {UserId}", userId);

        // Get user info (DAO001)
        var user = await QuerySingleAsync<User>("User.GetUserById", new { Id = userId });
        if (user == null)
        {
            throw new InvalidOperationException($"User {userId} not found");
        }

        // Get user's orders (DAO003)
        var orders = (await QueryListAsync<Order>("Order.GetOrdersByUser", new { UserId = userId })).ToList();

        // Get product details for each order (DAO002)
        var orderSummaries = new List<OrderSummaryItem>();
        foreach (var order in orders)
        {
            var items = await QueryListAsync<OrderItem>("Order.GetOrderItems", new { OrderId = order.Id });
            var itemDetails = new List<OrderItemDetail>();

            foreach (var item in items)
            {
                var product = await QuerySingleAsync<Product>("Product.GetProductById", new { Id = item.ProductId });
                if (product != null)
                {
                    itemDetails.Add(new OrderItemDetail
                    {
                        ProductId = item.ProductId,
                        ProductName = product.Name,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        Total = item.Quantity * item.Price
                    });
                }
            }

            orderSummaries.Add(new OrderSummaryItem
            {
                OrderId = order.Id,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Items = itemDetails
            });
        }

        return new UserOrderSummary
        {
            UserId = user.Id,
            UserName = user.Name,
            Email = user.Email,
            TotalOrders = orders.Count,
            TotalSpent = orders.Sum(o => o.TotalAmount),
            Orders = orderSummaries
        };
    }

    #endregion
}

#region Supporting Models

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class UserOrderSummary
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public List<OrderSummaryItem> Orders { get; set; } = new();
}

public class OrderSummaryItem
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItemDetail> Items { get; set; } = new();
}

public class OrderItemDetail
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total { get; set; }
}

#endregion
