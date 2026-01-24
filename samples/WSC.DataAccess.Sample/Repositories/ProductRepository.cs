using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Sample.Models;
using WSC.DataAccess.Core;
using WSC.DataAccess.Repository;

namespace WSC.DataAccess.Sample.Repositories;

/// <summary>
/// Product Repository - DAO002
/// Product management operations
/// </summary>
public class ProductRepository : ProviderBasedRepository<dynamic>
{
    private const string DAO_NAME = Provider.DAO002;
    private readonly ILogger<ProductRepository> _logger;

    public ProductRepository(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider,
        ILogger<ProductRepository> logger)
        : base(sessionFactory, provider, DAO_NAME)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<dynamic>> GetAllProductsAsync()
    {
        _logger.LogInformation("Getting all products");
        return await QueryListAsync("Product.GetAllProducts");
    }

    public async Task<dynamic?> GetProductByIdAsync(int id)
    {
        _logger.LogInformation("Getting product by ID: {ProductId}", id);
        return await QuerySingleAsync("Product.GetProductById", new { Id = id });
    }

    public async Task<IEnumerable<dynamic>> GetActiveProductsAsync()
    {
        _logger.LogInformation("Getting active products");
        return await QueryListAsync("Product.GetActiveProducts");
    }

    public async Task<IEnumerable<dynamic>> GetProductsInStockAsync()
    {
        _logger.LogInformation("Getting products in stock");
        return await QueryListAsync("Product.GetProductsInStock");
    }

    public async Task<int> GetProductCountAsync()
    {
        var result = await QuerySingleAsync("Product.GetProductCount");
        return Convert.ToInt32(result ?? 0);
    }
}
