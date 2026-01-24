using Microsoft.Extensions.Logging;
using WSC.DataAccess.Core;
using WSC.DataAccess.Mapping;
using WSC.DataAccess.Repository;

namespace WSC.DataAccess.RealDB.Test.Repositories;

/// <summary>
/// Example: Repository chỉ load GenericMap.xml
/// Hoàn toàn độc lập với ApplicationMap.xml
/// </summary>
public class ScopedGenericRepository : ScopedSqlMapRepository<dynamic>
{
    // Chỉ định SQL map file riêng
    private const string SQL_MAP_FILE = "SqlMaps/GenericMap.xml";

    public ScopedGenericRepository(
        IDbSessionFactory sessionFactory,
        ILogger<SqlMapConfig>? loggerConfig = null,
        ILogger<SqlMapper>? loggerMapper = null,
        ILogger<ScopedGenericRepository>? logger = null)
        : base(sessionFactory, SQL_MAP_FILE, loggerConfig, loggerMapper, logger)
    {
        // Repository này CHỈ load GenericMap.xml
    }

    public async Task<IEnumerable<dynamic>> GetTableNamesAsync()
    {
        return await QueryListAsync("Generic.GetTableNames");
    }

    public async Task<IEnumerable<dynamic>> GetColumnsAsync(string tableName)
    {
        return await QueryListAsync("Generic.GetColumns", new { TableName = tableName });
    }
}
