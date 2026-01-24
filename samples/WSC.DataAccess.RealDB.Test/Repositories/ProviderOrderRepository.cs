using WSC.DataAccess.Configuration;
using WSC.DataAccess.Core;
using WSC.DataAccess.Repository;

namespace WSC.DataAccess.RealDB.Test.Repositories;

/// <summary>
/// Example: Repository sử dụng SqlMapProvider (provider pattern)
/// Giống cách MrFu.Smartcheck sử dụng provider
/// </summary>
public class ProviderOrderRepository : ProviderBasedRepository<dynamic>
{
    // Key để lấy file từ provider
    private const string MAP_KEY = "Order";

    /// <summary>
    /// Constructor - File path được lấy từ SqlMapProvider
    /// </summary>
    public ProviderOrderRepository(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider)
        : base(sessionFactory, provider, MAP_KEY)
    {
        // File path tự động được lấy từ provider.GetFilePath("Order")
        // Không cần hardcode file path ở đây!
    }

    public async Task<IEnumerable<dynamic>> GetAllOrdersAsync()
    {
        return await QueryListAsync("Order.GetAll");
    }

    public async Task<dynamic?> GetByIdAsync(int id)
    {
        return await QuerySingleAsync("Order.GetById", new { Id = id });
    }

    public async Task<int> CreateAsync(dynamic order)
    {
        return await ExecuteAsync("Order.Insert", order);
    }
}
