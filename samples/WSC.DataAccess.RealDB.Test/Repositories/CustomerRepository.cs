using Microsoft.Extensions.Logging;
using WSC.DataAccess.Constants;
using WSC.DataAccess.Core;
using WSC.DataAccess.Mapping;
using WSC.DataAccess.Repository;

namespace WSC.DataAccess.RealDB.Test.Repositories;

/// <summary>
/// Example: Customer Repository sử dụng DAO010.xml
/// </summary>
public class CustomerRepository : ScopedSqlMapRepository<dynamic>
{
    // ✨ Constant: SqlMapFiles.DAO010
    private const string SQL_MAP_FILE = SqlMapFiles.DAO010;

    public CustomerRepository(
        IDbSessionFactory sessionFactory,
        ILogger<SqlMapConfig>? loggerConfig = null,
        ILogger<SqlMapper>? loggerMapper = null,
        ILogger<CustomerRepository>? logger = null)
        : base(sessionFactory, SQL_MAP_FILE, loggerConfig, loggerMapper, logger)
    {
        // Repository này CHỈ load DAO010.xml
    }

    public async Task<IEnumerable<dynamic>> GetAllCustomersAsync()
    {
        return await QueryListAsync("Customer.GetAll");
    }
}
