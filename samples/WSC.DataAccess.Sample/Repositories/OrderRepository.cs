using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Constants;
using WSC.DataAccess.Core;
using WSC.DataAccess.Repository;

namespace WSC.DataAccess.Sample.Repositories;

/// <summary>
/// Order Repository - DAO003
/// Order management operations
/// </summary>
public class OrderRepository : ProviderBasedRepository<dynamic>
{
    private const string DAO_NAME = DaoNames.DAO003;
    private readonly ILogger<OrderRepository> _logger;

    public OrderRepository(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider,
        ILogger<OrderRepository> logger)
        : base(sessionFactory, provider, DAO_NAME)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<dynamic>> GetAllOrdersAsync()
    {
        _logger.LogInformation("Getting all orders");
        return await QueryListAsync("Order.GetAllOrders");
    }

    public async Task<dynamic?> GetOrderByIdAsync(int id)
    {
        _logger.LogInformation("Getting order by ID: {OrderId}", id);
        return await QuerySingleAsync("Order.GetOrderById", new { Id = id });
    }

    public async Task<IEnumerable<dynamic>> GetOrdersByUserIdAsync(int userId)
    {
        _logger.LogInformation("Getting orders for user: {UserId}", userId);
        return await QueryListAsync("Order.GetOrdersByUserId", new { UserId = userId });
    }

    public async Task<IEnumerable<dynamic>> GetPendingOrdersAsync()
    {
        _logger.LogInformation("Getting pending orders");
        return await QueryListAsync("Order.GetPendingOrders");
    }

    public async Task<int> GetOrderCountAsync()
    {
        var result = await QuerySingleAsync("Order.GetOrderCount");
        return Convert.ToInt32(result ?? 0);
    }

    public async Task<decimal> GetTotalSalesAsync()
    {
        var result = await QuerySingleAsync("Order.GetTotalSales");
        return Convert.ToDecimal(result ?? 0);
    }
}
