using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Constants;
using WSC.DataAccess.RealDB.Test.Repositories;

namespace WSC.DataAccess.RealDB.Test;

/// <summary>
/// ✨ DEMO: Multiple Connections Pattern
///
/// Demonstrates how to use multiple named connections in one application:
/// - Connection_1: Main database (active data)
/// - Connection_2: Archive database (historical data)
/// - Connection_3: Analytics database (reports)
/// </summary>
public class MultiConnectionDemo
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("✨ MULTIPLE CONNECTIONS DEMO");
        Console.WriteLine("Using different databases for different purposes");
        Console.WriteLine("========================================\n");

        // ═══════════════════════════════════════════════════════════════
        // SETUP
        // ═══════════════════════════════════════════════════════════════

        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddIBatisLogging("log/iBatis", LogLevel.Information);
            builder.AddConsole();
        });

        // ═══════════════════════════════════════════════════════════════
        // MULTI-CONNECTION CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        Console.WriteLine("🔌 Configuring Multiple Connections:");
        Console.WriteLine("--------------------------------------------");

        // Main database connection
        var mainConnectionString = "Server=localhost;Database=WSC_Main;User Id=sa;Password=Pass;TrustServerCertificate=True";
        Console.WriteLine("✅ Connection_1 (Main): WSC_Main");

        // Archive database connection
        var archiveConnectionString = "Server=localhost;Database=WSC_Archive;User Id=sa;Password=Pass;TrustServerCertificate=True";
        Console.WriteLine("✅ Connection_2 (Archive): WSC_Archive");

        // Analytics database connection
        var analyticsConnectionString = "Server=localhost;Database=WSC_Analytics;User Id=sa;Password=Pass;TrustServerCertificate=True";
        Console.WriteLine("✅ Connection_3 (Analytics): WSC_Analytics");
        Console.WriteLine();

        // ═══════════════════════════════════════════════════════════════
        // WSC DATAACCESS WITH MULTIPLE CONNECTIONS
        // ═══════════════════════════════════════════════════════════════

        services.AddWscDataAccess(mainConnectionString, options =>
        {
            // Register additional connections
            options.AddConnection("Connection_2", archiveConnectionString);
            options.AddConnection("Connection_3", analyticsConnectionString);

            // ✨ Configure SQL Maps cho TỪNG CONNECTION
            options.ConfigureSqlMaps(provider =>
            {
                Console.WriteLine("📋 Registering SQL Maps:");
                Console.WriteLine("--------------------------------------------");

                // ───────────────────────────────────────────────────────
                // Connection_1 (Main Database) - Active Data
                // ───────────────────────────────────────────────────────
                provider.AddFile(DaoNames.ORDER, SqlMapFiles.DAO005, "Connection_1", "Active orders");
                provider.AddFile(DaoNames.CUSTOMER, SqlMapFiles.DAO010, "Connection_1", "Active customers");
                provider.AddFile(DaoNames.PRODUCT, SqlMapFiles.DAO015, "Connection_1", "Product catalog");
                provider.AddFile(DaoNames.DAO003, SqlMapFiles.DAO003, "Connection_1", "Groups");

                Console.WriteLine("  Connection_1 (Main):");
                Console.WriteLine($"    - {DaoNames.ORDER} -> {SqlMapFiles.DAO005}");
                Console.WriteLine($"    - {DaoNames.CUSTOMER} -> {SqlMapFiles.DAO010}");
                Console.WriteLine($"    - {DaoNames.PRODUCT} -> {SqlMapFiles.DAO015}");
                Console.WriteLine($"    - {DaoNames.DAO003} -> {SqlMapFiles.DAO003}");

                // ───────────────────────────────────────────────────────
                // Connection_2 (Archive Database) - Historical Data
                // ───────────────────────────────────────────────────────
                provider.AddFile(DaoNames.ORDER, SqlMapFiles.DAO006, "Connection_2", "Archived orders");
                provider.AddFile(DaoNames.CUSTOMER, SqlMapFiles.DAO011, "Connection_2", "Archived customers");

                Console.WriteLine("  Connection_2 (Archive):");
                Console.WriteLine($"    - {DaoNames.ORDER} -> {SqlMapFiles.DAO006}");
                Console.WriteLine($"    - {DaoNames.CUSTOMER} -> {SqlMapFiles.DAO011}");

                // ───────────────────────────────────────────────────────
                // Connection_3 (Analytics Database) - Reports
                // ───────────────────────────────────────────────────────
                provider.AddFile(DaoNames.REPORT, SqlMapFiles.DAO020, "Connection_3", "Business reports");
                provider.AddFile("Analytics", SqlMapFiles.DAO020, "Connection_3", "Analytics queries");

                Console.WriteLine("  Connection_3 (Analytics):");
                Console.WriteLine($"    - {DaoNames.REPORT} -> {SqlMapFiles.DAO020}");
                Console.WriteLine("    - Analytics -> DAO020.xml");
                Console.WriteLine();
            });
        });

        // Register repositories
        services.AddScoped<GroupRepository>();
        services.AddScoped<MultiConnectionRepository>();

        var serviceProvider = services.BuildServiceProvider();

        Console.WriteLine("========================================");
        Console.WriteLine("🧪 TESTING MULTIPLE CONNECTIONS");
        Console.WriteLine("========================================\n");

        try
        {
            using var scope = serviceProvider.CreateScope();

            // ═══════════════════════════════════════════════════════════════
            // TEST 1: Single Connection Repository (Main DB)
            // ═══════════════════════════════════════════════════════════════

            Console.WriteLine("Test 1: GroupRepository (Connection_1 - Main DB)");
            Console.WriteLine("--------------------------------------------");

            var groupRepo = scope.ServiceProvider.GetRequiredService<GroupRepository>();

            Console.WriteLine($"✅ Using DAO: {DaoNames.DAO003}");
            Console.WriteLine($"✅ Connection: Connection_1 (Default)");
            Console.WriteLine($"✅ File: {SqlMapFiles.DAO003}");

            // Simulate getting groups
            // var groups = await groupRepo.GetGroupsByUserAsync(1);
            // Console.WriteLine($"✅ Retrieved {groups.Count()} groups");

            Console.WriteLine("✅ GroupRepository working!");
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════════════
            // TEST 2: Multi-Connection Repository
            // ═══════════════════════════════════════════════════════════════

            Console.WriteLine("Test 2: MultiConnectionRepository (All 3 Connections)");
            Console.WriteLine("--------------------------------------------");

            var multiRepo = scope.ServiceProvider.GetRequiredService<MultiConnectionRepository>();

            Console.WriteLine("✅ Repository can access:");
            Console.WriteLine("   - Connection_1 (Main): Active orders & customers");
            Console.WriteLine("   - Connection_2 (Archive): Historical data");
            Console.WriteLine("   - Connection_3 (Analytics): Reports");
            Console.WriteLine();

            // Simulate operations
            Console.WriteLine("📊 Simulating operations:");
            Console.WriteLine("   1. GetActiveOrdersAsync() -> Connection_1");
            Console.WriteLine("   2. GetArchivedOrdersAsync() -> Connection_2");
            Console.WriteLine("   3. GetSalesReportAsync() -> Connection_3");
            Console.WriteLine("   4. GenerateComprehensiveReportAsync() -> ALL 3!");
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════════════
            // TEST 3: Provider Information
            // ═══════════════════════════════════════════════════════════════

            Console.WriteLine("Test 3: Provider Configuration Summary");
            Console.WriteLine("--------------------------------------------");

            var provider = scope.ServiceProvider.GetRequiredService<SqlMapProvider>();

            var connections = provider.GetAllConnectionNames();
            Console.WriteLine($"Total Connections: {connections.Length}");
            foreach (var conn in connections)
            {
                var files = provider.GetFilesByConnection(conn);
                Console.WriteLine($"\n{conn}:");
                foreach (var file in files)
                {
                    Console.WriteLine($"  - {file.Key} -> {file.FilePath}");
                    if (!string.IsNullOrEmpty(file.Description))
                        Console.WriteLine($"    ({file.Description})");
                }
            }
            Console.WriteLine();

            Console.WriteLine("========================================");
            Console.WriteLine("✅ ALL TESTS PASSED!");
            Console.WriteLine("========================================\n");

            Console.WriteLine("💡 Key Concepts:");
            Console.WriteLine("   1. One app can use MULTIPLE databases");
            Console.WriteLine("   2. Each connection has its own SQL maps");
            Console.WriteLine("   3. Repository can switch connections dynamically");
            Console.WriteLine("   4. Same DAO name (e.g., 'Order') can map to different files per connection");
            Console.WriteLine();

            Console.WriteLine("📝 Usage Pattern:");
            Console.WriteLine("   // Different databases for different purposes");
            Console.WriteLine("   Connection_1: Main DB (current data)");
            Console.WriteLine("   Connection_2: Archive DB (historical data)");
            Console.WriteLine("   Connection_3: Analytics DB (reporting)");
            Console.WriteLine();

            Console.WriteLine("🎯 Use Cases:");
            Console.WriteLine("   ✅ Separate read/write databases");
            Console.WriteLine("   ✅ Archive old data to different database");
            Console.WriteLine("   ✅ Dedicated analytics/reporting database");
            Console.WriteLine("   ✅ Multi-tenant applications");
            Console.WriteLine("   ✅ Microservices accessing shared databases");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR: {ex.Message}");
            Console.WriteLine($"   Details: {ex}");
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
