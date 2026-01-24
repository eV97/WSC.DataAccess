using Microsoft.Extensions.Logging;
using WSC.DataAccess.Constants;
using WSC.DataAccess.Core;
using WSC.DataAccess.Mapping;
using WSC.DataAccess.Repository;

namespace WSC.DataAccess.RealDB.Test.Repositories;

/// <summary>
/// Example: Product Repository sử dụng DAO015.xml
/// </summary>
public class ProductRepository : ScopedSqlMapRepository<dynamic>
{
    // ✨ Constant: SqlMapFiles.DAO015
    private const string SQL_MAP_FILE = SqlMapFiles.DAO015;

    public ProductRepository(
        IDbSessionFactory sessionFactory,
        ILogger<SqlMapConfig>? loggerConfig = null,
        ILogger<SqlMapper>? loggerMapper = null,
        ILogger<ProductRepository>? logger = null)
        : base(sessionFactory, SQL_MAP_FILE, loggerConfig, loggerMapper, logger)
    {
        // Repository này CHỈ load DAO015.xml
    }

    public async Task<IEnumerable<dynamic>> GetAllProductsAsync()
    {
        return await QueryListAsync("Product.GetAll");
    }
}
