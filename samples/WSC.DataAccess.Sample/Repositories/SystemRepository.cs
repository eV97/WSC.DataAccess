using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Constants;
using WSC.DataAccess.Core;
using WSC.DataAccess.Repository;

namespace WSC.DataAccess.Sample.Repositories;

/// <summary>
/// System Repository - DAO000
/// System information queries
/// </summary>
public class SystemRepository : ProviderBasedRepository<dynamic>
{
    private const string DAO_NAME = DaoNames.DAO000;
    private readonly ILogger<SystemRepository> _logger;

    public SystemRepository(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider,
        ILogger<SystemRepository> logger)
        : base(sessionFactory, provider, DAO_NAME)
    {
        _logger = logger;
    }

    public async Task<string> GetDatabaseVersionAsync()
    {
        var result = await QuerySingleAsync("System.GetDatabaseVersion");
        return result?.ToString() ?? "Unknown";
    }

    public async Task<string> GetCurrentDatabaseAsync()
    {
        var result = await QuerySingleAsync("System.GetCurrentDatabase");
        return result?.ToString() ?? "Unknown";
    }

    public async Task<string> GetCurrentUserAsync()
    {
        var result = await QuerySingleAsync("System.GetCurrentUser");
        return result?.ToString() ?? "Unknown";
    }

    public async Task<string> GetServerNameAsync()
    {
        var result = await QuerySingleAsync("System.GetServerName");
        return result?.ToString() ?? "Unknown";
    }

    public async Task<DateTime> GetCurrentDateTimeAsync()
    {
        var result = await QuerySingleAsync("System.GetCurrentDateTime");
        return (DateTime)(result ?? DateTime.Now);
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var result = await QuerySingleAsync("System.TestQuery");
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed");
            return false;
        }
    }

    public async Task<IEnumerable<dynamic>> GetDatabaseSizeAsync()
    {
        return await QueryListAsync("System.GetDatabaseSize");
    }

    public async Task<IEnumerable<dynamic>> ListAllTablesAsync()
    {
        return await QueryListAsync("System.ListAllTables");
    }

    public async Task<int> GetTableCountAsync()
    {
        var result = await QuerySingleAsync("System.GetTableCount");
        return Convert.ToInt32(result ?? 0);
    }

    public async Task<int> GetConnectionCountAsync()
    {
        var result = await QuerySingleAsync("System.GetConnectionCount");
        return Convert.ToInt32(result ?? 0);
    }
}
