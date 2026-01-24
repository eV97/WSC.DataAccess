using WSC.DataAccess.Attributes;
using WSC.DataAccess.Constants;
using WSC.DataAccess.Core;
using WSC.DataAccess.Repository;

namespace WSC.DataAccess.RealDB.Test.Repositories;

/// <summary>
/// ✨ Customer Repository - Đơn giản với attribute
/// </summary>
[SqlMapFile(SqlMapFiles.DAO010)]  // ← Chỉ cần dòng này!
public class SimpleCustomerRepository : SimpleSqlMapRepository<dynamic>
{
    public SimpleCustomerRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory)
    {
    }

    public async Task<IEnumerable<dynamic>> GetAllAsync()
    {
        return await QueryListAsync("Customer.GetAll");
    }
}
