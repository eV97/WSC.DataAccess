using Microsoft.Extensions.Logging;
using WSC.DataAccess.Constants;
using WSC.DataAccess.Core;
using WSC.DataAccess.Mapping;
using WSC.DataAccess.Repository;

namespace WSC.DataAccess.RealDB.Test.Repositories;

/// <summary>
/// Example: Order Repository sử dụng DAO005.xml
/// Chỉ cần gõ: SqlMapFiles.DAO005
/// </summary>
public class OrderRepository : ScopedSqlMapRepository<dynamic>
{
    // ✨ Sử dụng constant - dễ dàng, có IntelliSense
    private const string SQL_MAP_FILE = SqlMapFiles.DAO005;

    public OrderRepository(
        IDbSessionFactory sessionFactory,
        ILogger<SqlMapConfig>? loggerConfig = null,
        ILogger<SqlMapper>? loggerMapper = null,
        ILogger<OrderRepository>? logger = null)
        : base(sessionFactory, SQL_MAP_FILE, loggerConfig, loggerMapper, logger)
    {
        // Repository này CHỈ load DAO005.xml
    }

    // Example methods
    public async Task<IEnumerable<dynamic>> GetAllOrdersAsync()
    {
        return await QueryListAsync("Order.GetAll");
    }

    public async Task<dynamic?> GetByIdAsync(int id)
    {
        return await QuerySingleAsync("Order.GetById", new { Id = id });
    }
}
