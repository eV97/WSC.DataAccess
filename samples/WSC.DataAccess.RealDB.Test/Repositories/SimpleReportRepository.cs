using WSC.DataAccess.Attributes;
using WSC.DataAccess.Constants;
using WSC.DataAccess.Core;
using WSC.DataAccess.Repository;

namespace WSC.DataAccess.RealDB.Test.Repositories;

/// <summary>
/// ✨ Report Repository - Sử dụng NHIỀU files với attribute
/// </summary>
[SqlMapFiles(SqlMapFiles.DAO005, SqlMapFiles.DAO010, SqlMapFiles.DAO015)]
//           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//           Nhiều files - Chỉ cần list trong attribute!
public class SimpleReportRepository : SimpleSqlMapRepository<dynamic>
{
    public SimpleReportRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory)
    {
        // Tự động load 3 files: DAO005, DAO010, DAO015
    }

    public async Task<IEnumerable<dynamic>> GetOrdersAsync()
    {
        return await QueryListAsync("Order.GetAll");
    }

    public async Task<IEnumerable<dynamic>> GetCustomersAsync()
    {
        return await QueryListAsync("Customer.GetAll");
    }

    public async Task<IEnumerable<dynamic>> GetProductsAsync()
    {
        return await QueryListAsync("Product.GetAll");
    }
}
