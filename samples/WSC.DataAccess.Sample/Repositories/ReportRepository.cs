using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Sample.Models;
using WSC.DataAccess.Core;
using WSC.DataAccess.Repository;

namespace WSC.DataAccess.Sample.Repositories;

/// <summary>
/// Report Repository - DAO005
/// Analytics and reporting operations
/// </summary>
public class ReportRepository : ProviderBasedRepository<dynamic>
{
    private const string DAO_NAME = Provider.DAO005;
    private readonly ILogger<ReportRepository> _logger;

    public ReportRepository(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider,
        ILogger<ReportRepository> logger)
        : base(sessionFactory, provider, DAO_NAME)
    {
        _logger = logger;
    }

    public async Task<dynamic?> GetSalesSummaryAsync()
    {
        _logger.LogInformation("Getting sales summary");
        return await QuerySingleAsync("Report.GetSalesSummary");
    }

    public async Task<IEnumerable<dynamic>> GetSalesByDateAsync()
    {
        _logger.LogInformation("Getting sales by date");
        return await QueryListAsync("Report.GetSalesByDate");
    }

    public async Task<IEnumerable<dynamic>> GetSalesByMonthAsync()
    {
        _logger.LogInformation("Getting sales by month");
        return await QueryListAsync("Report.GetSalesByMonth");
    }

    public async Task<IEnumerable<dynamic>> GetTopCustomersAsync(int limit = 10)
    {
        _logger.LogInformation("Getting top {Limit} customers", limit);
        return await QueryListAsync("Report.GetTopCustomers", new { Limit = limit });
    }

    public async Task<IEnumerable<dynamic>> GetTopProductsAsync(int limit = 10)
    {
        _logger.LogInformation("Getting top {Limit} products", limit);
        return await QueryListAsync("Report.GetTopProducts", new { Limit = limit });
    }

    public async Task<dynamic?> GetInventorySummaryAsync()
    {
        _logger.LogInformation("Getting inventory summary");
        return await QuerySingleAsync("Report.GetInventorySummary");
    }

    public async Task<dynamic?> GetUserStatisticsAsync()
    {
        _logger.LogInformation("Getting user statistics");
        return await QuerySingleAsync("Report.GetUserStatistics");
    }

    public async Task<dynamic?> GetOrderStatisticsAsync()
    {
        _logger.LogInformation("Getting order statistics");
        return await QuerySingleAsync("Report.GetOrderStatistics");
    }

    public async Task<IEnumerable<dynamic>> GetRevenueByCategoryAsync()
    {
        _logger.LogInformation("Getting revenue by category");
        return await QueryListAsync("Report.GetRevenueByCategory");
    }

    public async Task<IEnumerable<dynamic>> GetProductPerformanceAsync()
    {
        _logger.LogInformation("Getting product performance");
        return await QueryListAsync("Report.GetProductPerformance");
    }
}
