using WSC.DataAccess.Attributes;
using WSC.DataAccess.Constants;
using WSC.DataAccess.Core;
using WSC.DataAccess.Repository;

namespace WSC.DataAccess.RealDB.Test.Repositories;

/// <summary>
/// ✨ CÁCH ĐƠN GIẢN NHẤT - Chỉ cần attribute!
///
/// [SqlMapFile(SqlMapFiles.DAO005)]  ← Chỉ cần dòng này!
/// public class SimpleOrderRepository : SimpleSqlMapRepository&lt;dynamic&gt; { }
///
/// Không cần:
/// - const string SQL_MAP_FILE
/// - base constructor với parameters
/// - Gì cả!
/// </summary>
[SqlMapFile(SqlMapFiles.DAO005)]
public class SimpleOrderRepository : SimpleSqlMapRepository<dynamic>
{
    // ✨ Chỉ cần constructor đơn giản
    public SimpleOrderRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory)
    {
        // File DAO005.xml tự động được load từ attribute!
    }

    // Các methods như bình thường
    public async Task<IEnumerable<dynamic>> GetAllAsync()
    {
        return await QueryListAsync("Order.GetAll");
    }

    public async Task<dynamic?> GetByIdAsync(int id)
    {
        return await QuerySingleAsync("Order.GetById", new { Id = id });
    }
}
