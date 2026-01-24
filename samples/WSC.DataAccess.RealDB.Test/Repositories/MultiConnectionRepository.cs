using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Constants;
using WSC.DataAccess.Core;
using WSC.DataAccess.Repository;

namespace WSC.DataAccess.RealDB.Test.Repositories;

/// <summary>
/// ✨ Example: Repository sử dụng NHIỀU CONNECTIONS
/// Connection_1: Main database (Orders, Customers)
/// Connection_2: Archive database (Historical data)
/// Connection_3: Analytics database (Reports)
/// </summary>
public class MultiConnectionRepository : ProviderBasedRepository<dynamic>
{
    // Connection names
    private const string CONNECTION_MAIN = "Connection_1";
    private const string CONNECTION_ARCHIVE = "Connection_2";
    private const string CONNECTION_ANALYTICS = "Connection_3";

    // DAO names
    private const string DAO_ORDER = DaoNames.DAO005;
    private const string DAO_CUSTOMER = DaoNames.DAO010;

    private readonly ILogger<MultiConnectionRepository> _logger;
    private readonly IDbSessionFactory _sessionFactory;
    private readonly SqlMapProvider _provider;

    public MultiConnectionRepository(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider,
        ILogger<MultiConnectionRepository> logger)
        : base(sessionFactory, provider, DAO_ORDER, CONNECTION_MAIN)
    {
        _sessionFactory = sessionFactory;
        _provider = provider;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════
    // Main Database Operations (Connection_1)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Lấy orders từ MAIN database
    /// </summary>
    public async Task<IEnumerable<dynamic>> GetActiveOrdersAsync()
    {
        try
        {
            _logger.LogInformation("Getting active orders from MAIN database");

            // ✨ Sử dụng Connection_1 (Main database)
            return await QueryListAsync("Order.GetAll", null, CONNECTION_MAIN);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active orders from main database");
            throw;
        }
    }

    /// <summary>
    /// Lấy customers từ MAIN database
    /// </summary>
    public async Task<IEnumerable<dynamic>> GetActiveCustomersAsync()
    {
        try
        {
            _logger.LogInformation("Getting active customers from MAIN database");

            // ✨ Sử dụng Connection_1 (Main database)
            return await QueryListAsync("Customer.GetAll", null, CONNECTION_MAIN);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active customers from main database");
            throw;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Archive Database Operations (Connection_2)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Lấy archived orders từ ARCHIVE database
    /// </summary>
    public async Task<IEnumerable<dynamic>> GetArchivedOrdersAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            _logger.LogInformation("Getting archived orders from ARCHIVE database: {FromDate} to {ToDate}",
                fromDate, toDate);

            var parameters = new { FromDate = fromDate, ToDate = toDate };

            // ✨ Sử dụng Connection_2 (Archive database)
            return await QueryListAsync("Order.GetArchived", parameters, CONNECTION_ARCHIVE);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting archived orders");
            throw;
        }
    }

    /// <summary>
    /// Lấy archived customers từ ARCHIVE database
    /// </summary>
    public async Task<IEnumerable<dynamic>> GetArchivedCustomersAsync(int year)
    {
        try
        {
            _logger.LogInformation("Getting archived customers from ARCHIVE database: Year {Year}", year);

            var parameters = new { Year = year };

            // ✨ Sử dụng Connection_2 (Archive database)
            return await QueryListAsync("Customer.GetArchived", parameters, CONNECTION_ARCHIVE);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting archived customers");
            throw;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Analytics Database Operations (Connection_3)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Lấy sales report từ ANALYTICS database
    /// </summary>
    public async Task<IEnumerable<dynamic>> GetSalesReportAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            _logger.LogInformation("Getting sales report from ANALYTICS database: {FromDate} to {ToDate}",
                fromDate, toDate);

            var parameters = new { FromDate = fromDate, ToDate = toDate };

            // ✨ Sử dụng Connection_3 (Analytics database)
            return await QueryListAsync("Report.SalesByPeriod", parameters, CONNECTION_ANALYTICS);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales report");
            throw;
        }
    }

    /// <summary>
    /// Lấy customer analytics từ ANALYTICS database
    /// </summary>
    public async Task<IEnumerable<dynamic>> GetCustomerAnalyticsAsync()
    {
        try
        {
            _logger.LogInformation("Getting customer analytics from ANALYTICS database");

            // ✨ Sử dụng Connection_3 (Analytics database)
            return await QueryListAsync("Report.CustomerAnalytics", null, CONNECTION_ANALYTICS);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer analytics");
            throw;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Cross-Database Operations
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Lấy tất cả orders từ cả MAIN và ARCHIVE databases
    /// </summary>
    public async Task<IEnumerable<dynamic>> GetAllOrdersFromAllDatabasesAsync()
    {
        try
        {
            _logger.LogInformation("Getting orders from ALL databases");

            var allOrders = new List<dynamic>();

            // Lấy từ Main database
            var activeOrders = await QueryListAsync("Order.GetAll", null, CONNECTION_MAIN);
            allOrders.AddRange(activeOrders);

            // Lấy từ Archive database
            var archivedOrders = await QueryListAsync("Order.GetAll", null, CONNECTION_ARCHIVE);
            allOrders.AddRange(archivedOrders);

            _logger.LogInformation("Retrieved {Count} orders from all databases", allOrders.Count);

            return allOrders;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders from all databases");
            throw;
        }
    }

    /// <summary>
    /// Generate comprehensive report từ multiple databases
    /// </summary>
    public async Task<ComprehensiveReport> GenerateComprehensiveReportAsync()
    {
        try
        {
            _logger.LogInformation("Generating comprehensive report from multiple databases");

            var report = new ComprehensiveReport();

            // Data from Main database
            var activeOrders = await QueryListAsync("Order.GetAll", null, CONNECTION_MAIN);
            report.ActiveOrdersCount = activeOrders.Count();

            var activeCustomers = await QueryListAsync("Customer.GetAll", null, CONNECTION_MAIN);
            report.ActiveCustomersCount = activeCustomers.Count();

            // Data from Archive database
            var archivedOrders = await QueryListAsync("Order.GetAll", null, CONNECTION_ARCHIVE);
            report.ArchivedOrdersCount = archivedOrders.Count();

            // Data from Analytics database
            var salesReport = await QueryListAsync("Report.TotalSales", null, CONNECTION_ANALYTICS);
            report.TotalSales = salesReport.FirstOrDefault()?.TotalSales ?? 0;

            _logger.LogInformation("Comprehensive report generated successfully");

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating comprehensive report");
            throw;
        }
    }
}

/// <summary>
/// Comprehensive report model
/// </summary>
public class ComprehensiveReport
{
    public int ActiveOrdersCount { get; set; }
    public int ArchivedOrdersCount { get; set; }
    public int ActiveCustomersCount { get; set; }
    public decimal TotalSales { get; set; }

    public int TotalOrders => ActiveOrdersCount + ArchivedOrdersCount;
}
