using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.RealDB.Test.Repositories;

namespace WSC.DataAccess.RealDB.Test;

/// <summary>
/// Demo: Scoped SQL Map - Mỗi repository chỉ load file riêng của nó
/// </summary>
public class ScopedSqlMapDemo
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  Scoped SQL Map Demo                                             ║");
        Console.WriteLine("║  Mỗi Repository chỉ load SQL map file riêng của nó               ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝\n");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddIBatisLogging(minimumLevel: Serilog.Events.LogEventLevel.Information);
                Console.WriteLine("✓ Logging enabled\n");
            })
            .ConfigureServices((context, services) =>
            {
                // QUAN TRỌNG: KHÔNG cần AddSqlMapFile() ở đây!
                // Mỗi repository tự load file riêng của nó
                services.AddWscDataAccess(connectionString);

                // Đăng ký scoped repositories
                services.AddScoped<ScopedApplicationRepository>();
                services.AddScoped<ScopedGenericRepository>();
            })
            .Build();

        using (var scope = host.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            Console.WriteLine("═══════════════════════════════════════════════════════════════\n");
            Console.WriteLine("📋 Concept:");
            Console.WriteLine("   - ScopedApplicationRepository → CHỈ load ApplicationMap.xml");
            Console.WriteLine("   - ScopedGenericRepository     → CHỈ load GenericMap.xml");
            Console.WriteLine("   - Hoàn toàn độc lập, không ảnh hưởng lẫn nhau\n");

            Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

            // Test 1: ScopedApplicationRepository
            Console.WriteLine("Test 1: ScopedApplicationRepository (chỉ load ApplicationMap.xml)\n");

            try
            {
                var appRepo = services.GetRequiredService<ScopedApplicationRepository>();

                Console.WriteLine("   1️⃣  Getting all applications...");
                var apps = await appRepo.GetAllApplicationsAsync();
                var appsList = apps.ToList();

                Console.WriteLine($"      ✓ Found {appsList.Count} applications");

                if (appsList.Any())
                {
                    Console.WriteLine("      Sample:");
                    foreach (var app in appsList.Take(2))
                    {
                        Console.WriteLine($"         - {app.ApplicationName}");
                    }
                }

                Console.WriteLine();
                Console.WriteLine("   2️⃣  Getting application by ID...");
                var singleApp = await appRepo.GetByIdAsync(1);

                if (singleApp != null)
                {
                    Console.WriteLine($"      ✓ Found: {singleApp.ApplicationName}");
                }
                else
                {
                    Console.WriteLine("      ⚠️  Application ID 1 not found");
                }

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      ❌ Error: {ex.Message}");
                Console.WriteLine("      Tip: Check if ApplicationMap.xml exists and database is accessible");
            }

            Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

            // Test 2: ScopedGenericRepository
            Console.WriteLine("Test 2: ScopedGenericRepository (chỉ load GenericMap.xml)\n");

            try
            {
                var genericRepo = services.GetRequiredService<ScopedGenericRepository>();

                Console.WriteLine("   1️⃣  Getting table names...");
                var tables = await genericRepo.GetTableNamesAsync();
                var tablesList = tables.ToList();

                Console.WriteLine($"      ✓ Found {tablesList.Count} tables");

                if (tablesList.Any())
                {
                    Console.WriteLine("      Sample:");
                    foreach (var table in tablesList.Take(3))
                    {
                        Console.WriteLine($"         - {table.TABLE_NAME}");
                    }
                }

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      ❌ Error: {ex.Message}");
                Console.WriteLine("      Tip: Check if GenericMap.xml exists");
            }

            Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

            // Show isolation
            Console.WriteLine("🔒 Isolation Demo:\n");
            Console.WriteLine("   Giả sử GenericMap.xml có lỗi:");
            Console.WriteLine("   ❌ ScopedGenericRepository → Bị lỗi");
            Console.WriteLine("   ✅ ScopedApplicationRepository → Vẫn hoạt động bình thường!");
            Console.WriteLine();
            Console.WriteLine("   Vì mỗi repository load file riêng của nó,");
            Console.WriteLine("   lỗi ở file này KHÔNG ảnh hưởng file khác.\n");

            Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

            Console.WriteLine("✅ Demo completed!\n");

            Console.WriteLine("📁 Check logs:");
            var logDate = DateTime.Now.ToString("yyyyMMdd");
            Console.WriteLine($"   log/iBatis/ibatis-{logDate}.log\n");

            Console.WriteLine("🔍 In logs, you will see:");
            Console.WriteLine("   [INF] Loading SQL map file: SqlMaps/ApplicationMap.xml");
            Console.WriteLine("   [INF] Loading SQL map file: SqlMaps/GenericMap.xml");
            Console.WriteLine("   → Each repository loads its own file!\n");

            Console.WriteLine("📚 Read more: SCOPED_SQLMAP_GUIDE.md\n");
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
