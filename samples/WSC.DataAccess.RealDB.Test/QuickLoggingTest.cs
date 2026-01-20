using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Core;
using WSC.DataAccess.Mapping;

namespace WSC.DataAccess.RealDB.Test;

/// <summary>
/// Quick test để verify logging hoạt động - chỉ cần chạy và check log files
/// </summary>
public class QuickLoggingTest
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  Quick Logging Test - iBatis                                    ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=.;Database=TestDB;Integrated Security=true;TrustServerCertificate=true;";

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddIBatisLogging(minimumLevel: Serilog.Events.LogEventLevel.Debug);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddWscDataAccess(connectionString, options => { });
            })
            .Build();

        Console.WriteLine("✓ Logging initialized\n");

        using (var scope = host.Services.CreateScope())
        {
            var sessionFactory = scope.ServiceProvider.GetRequiredService<IDbSessionFactory>();
            var config = scope.ServiceProvider.GetRequiredService<SqlMapConfig>();

            // Register test statement
            config.RegisterStatement(new SqlStatement
            {
                Id = "Test.SimpleQuery",
                CommandText = "SELECT 1 AS TestValue, 'Hello' AS Message",
                StatementType = SqlStatementType.Select
            });

            Console.WriteLine("Test Scenario: Simple Query with Transaction\n");

            // Test with transaction
            using (var session = sessionFactory.OpenSession())
            {
                Console.WriteLine("1. Starting transaction...");
                session.BeginTransaction();

                var sqlMapper = scope.ServiceProvider.GetRequiredService<SqlMapper>();

                Console.WriteLine("2. Executing query...");
                var result = await sqlMapper.QueryAsync<dynamic>(
                    session,
                    "Test.SimpleQuery",
                    null);

                Console.WriteLine($"   ✓ Got {result.Count()} row(s)");

                Console.WriteLine("3. Committing transaction...");
                session.Commit();

                Console.WriteLine("   ✓ Transaction committed\n");
            }

            // Test error scenario
            Console.WriteLine("Test Scenario: Error Handling\n");

            config.RegisterStatement(new SqlStatement
            {
                Id = "Test.ErrorQuery",
                CommandText = "SELECT * FROM TableThatDoesNotExist",
                StatementType = SqlStatementType.Select
            });

            try
            {
                using var session = sessionFactory.OpenSession();
                var sqlMapper = scope.ServiceProvider.GetRequiredService<SqlMapper>();

                Console.WriteLine("1. Executing query that will fail...");
                await sqlMapper.QueryAsync<dynamic>(session, "Test.ErrorQuery", null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ✓ Error caught: {ex.Message.Split('\n')[0]}\n");
            }

            // Test statement not found
            Console.WriteLine("Test Scenario: Statement Not Found\n");

            try
            {
                using var session = sessionFactory.OpenSession();
                var sqlMapper = scope.ServiceProvider.GetRequiredService<SqlMapper>();

                Console.WriteLine("1. Looking for non-existent statement...");
                await sqlMapper.QueryAsync<dynamic>(session, "Does.NotExist", null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ✓ Error caught: {ex.Message}\n");
            }
        }

        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  ✅ Tests completed!");
        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

        var logDate = DateTime.Now.ToString("yyyyMMdd");
        var fullLogPath = Path.Combine(Directory.GetCurrentDirectory(), "log", "iBatis", $"ibatis-{logDate}.log");
        var errorLogPath = Path.Combine(Directory.GetCurrentDirectory(), "log", "iBatis", $"ibatis-errors-{logDate}.log");

        Console.WriteLine("📁 Check these log files:\n");
        Console.WriteLine($"   Full logs:");
        Console.WriteLine($"   {fullLogPath}");
        Console.WriteLine();
        Console.WriteLine($"   Error logs:");
        Console.WriteLine($"   {errorLogPath}");
        Console.WriteLine();

        Console.WriteLine("🔍 Expected log entries:\n");
        Console.WriteLine("   Full log should contain:");
        Console.WriteLine("   ✓ [DBG] DbSession created - SessionId: xxxxxxxx");
        Console.WriteLine("   ✓ [INF] Connection opened - SessionId: xxxxxxxx");
        Console.WriteLine("   ✓ [INF] Transaction started - SessionId: xxxxxxxx");
        Console.WriteLine("   ✓ [DBG] Executing QueryAsync - StatementId: Test.SimpleQuery");
        Console.WriteLine("   ✓ [INF] QueryAsync completed - StatementId: Test.SimpleQuery, ResultCount: 1, Duration: XXms");
        Console.WriteLine("   ✓ [INF] Transaction committed successfully - SessionId: xxxxxxxx");
        Console.WriteLine("   ✓ [ERR] QueryAsync failed - StatementId: Test.ErrorQuery");
        Console.WriteLine("   ✓ [WRN] Statement not found: Does.NotExist");
        Console.WriteLine();

        Console.WriteLine("   Error log should contain:");
        Console.WriteLine("   ✓ [ERR] QueryAsync failed - StatementId: Test.ErrorQuery");
        Console.WriteLine("   ✓ Full exception stack trace");
        Console.WriteLine();

        // Try to display last few lines of logs
        if (File.Exists(fullLogPath))
        {
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine("  📝 Last 10 lines from full log:");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");

            try
            {
                var lines = File.ReadAllLines(fullLogPath);
                var lastLines = lines.TakeLast(10);

                foreach (var line in lastLines)
                {
                    Console.WriteLine(line);
                }

                Console.WriteLine();
            }
            catch
            {
                Console.WriteLine("   (Could not read log file - it may be locked)");
            }
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
