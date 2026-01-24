using Microsoft.Extensions.Logging;
using WSC.DataAccess.Constants;
using WSC.DataAccess.Core;
using WSC.DataAccess.Mapping;
using WSC.DataAccess.RealDB.Test.Models;
using WSC.DataAccess.Repository;

namespace WSC.DataAccess.RealDB.Test.Repositories;

/// <summary>
/// Example: Repository chỉ load SQL map file riêng của nó (ApplicationMap.xml)
/// Không load các SQL map files khác, tránh conflict
/// </summary>
public class ScopedApplicationRepository : ScopedSqlMapRepository<Application>
{
    // Sử dụng constant thay vì hardcoded string
    private const string SQL_MAP_FILE = SqlMapFiles.APPLICATION_MAP;

    /// <summary>
    /// Constructor - Tự động load chỉ DAO005.xml (ApplicationMap.xml)
    /// </summary>
    public ScopedApplicationRepository(
        IDbSessionFactory sessionFactory,
        ILogger<SqlMapConfig>? loggerConfig = null,
        ILogger<SqlMapper>? loggerMapper = null,
        ILogger<ScopedApplicationRepository>? logger = null)
        : base(sessionFactory, SQL_MAP_FILE, loggerConfig, loggerMapper, logger)
    {
        // Repository này CHỈ load ApplicationMap.xml
        // Không bị ảnh hưởng bởi các SQL map files khác
    }

    // Các methods sử dụng statements từ ApplicationMap.xml

    public async Task<IEnumerable<Application>> GetAllApplicationsAsync()
    {
        return await QueryListAsync("Application.GetAll");
    }

    public async Task<Application?> GetByIdAsync(int id)
    {
        return await QuerySingleAsync("Application.GetById", new { Id = id });
    }

    public async Task<IEnumerable<Application>> SearchByNameAsync(string name)
    {
        return await QueryListAsync("Application.SearchByName", new { Name = $"%{name}%" });
    }

    public async Task<int> InsertAsync(Application application)
    {
        return await ExecuteAsync("Application.Insert", application);
    }

    public async Task<int> UpdateAsync(Application application)
    {
        return await ExecuteAsync("Application.Update", application);
    }

    public async Task<int> DeleteAsync(int id)
    {
        return await ExecuteAsync("Application.Delete", new { Id = id });
    }

    public async Task<int> GetActiveCountAsync()
    {
        using var session = SessionFactory.OpenSession();
        var result = await SqlMapper.ExecuteScalarAsync<int>(
            session,
            "Application.GetActiveCount",
            null);
        return result;
    }
}
