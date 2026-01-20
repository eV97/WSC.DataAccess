using WSC.DataAccess.Core;
using WSC.DataAccess.Examples.Models;
using WSC.DataAccess.Mapping;
using WSC.DataAccess.Repository;

namespace WSC.DataAccess.Examples.Repositories;

/// <summary>
/// Example Product repository using SqlMapRepository (IBatis-style)
/// </summary>
public class ProductRepository : SqlMapRepository<Product>
{
    public ProductRepository(IDbSessionFactory sessionFactory, SqlMapper sqlMapper)
        : base(sessionFactory, sqlMapper)
    {
    }

    /// <summary>
    /// Gets all products
    /// </summary>
    public async Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        return await QueryListAsync("Product.GetAll");
    }

    /// <summary>
    /// Gets product by ID
    /// </summary>
    public async Task<Product?> GetByIdAsync(int id)
    {
        return await QuerySingleAsync("Product.GetById", new { Id = id });
    }

    /// <summary>
    /// Gets products by category
    /// </summary>
    public async Task<IEnumerable<Product>> GetByCategoryAsync(string category)
    {
        return await QueryListAsync("Product.GetByCategory", new { Category = category });
    }

    /// <summary>
    /// Inserts a new product
    /// </summary>
    public async Task<int> InsertAsync(Product product)
    {
        return await ExecuteAsync("Product.Insert", product);
    }

    /// <summary>
    /// Updates a product
    /// </summary>
    public async Task<int> UpdateAsync(Product product)
    {
        return await ExecuteAsync("Product.Update", product);
    }

    /// <summary>
    /// Deletes a product
    /// </summary>
    public async Task<int> DeleteAsync(int id)
    {
        return await ExecuteAsync("Product.Delete", new { Id = id });
    }

    /// <summary>
    /// Gets products with low stock
    /// </summary>
    public async Task<IEnumerable<Product>> GetLowStockProductsAsync(int threshold)
    {
        return await QueryListAsync("Product.GetLowStock", new { Threshold = threshold });
    }
}
