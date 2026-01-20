using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Core;
using WSC.DataAccess.Mapping;

namespace WSC.DataAccess.RealDB.Test;

/// <summary>
/// Simple log test - Chạy nhanh để xem logs
/// </summary>
public class LogTest
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  iBatis Log Test - Simple & Fast                                ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝\n");

        var connectionString = "Server=.;Database=TestDB;Integrated Security=true;TrustServerCertificate=true;";

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddIBatisLogging(minimumLevel: Serilog.Events.LogEventLevel.Debug);
                Console.WriteLine("✓ Logging enabled (Debug level)\n");
            })
            .ConfigureServices((context, services) =>
            {
                services.AddWscDataAccess(connectionString);
            })
            .Build();

        using (var scope = host.Services.CreateScope())
        {
            var sessionFactory = scope.ServiceProvider.GetRequiredService<IDbSessionFactory>();
            var config = scope.ServiceProvider.GetRequiredService<SqlMapConfig>();
            var sqlMapper = scope.ServiceProvider.GetRequiredService<SqlMapper>();

            Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

            // Test 1: Register statement
            Console.WriteLine("1️⃣  Registering SQL statement...");
            config.RegisterStatement(new SqlStatement
            {
                Id = "Test.SimpleQuery",
                CommandText = "SELECT 1 AS Id, 'Test' AS Name, GETDATE() AS CreatedDate",
                StatementType = SqlStatementType.Select
            });
            Console.WriteLine("   ✓ Statement registered\n");

            // Test 2: Open connection
            Console.WriteLine("2️⃣  Opening database session...");
            using (var session = sessionFactory.OpenSession())
            {
                Console.WriteLine("   ✓ Session opened\n");

                // Test 3: Start transaction
                Console.WriteLine("3️⃣  Starting transaction...");
                session.BeginTransaction();
                Console.WriteLine("   ✓ Transaction started\n");

                // Test 4: Execute query
                Console.WriteLine("4️⃣  Executing query...");
                var result = await sqlMapper.QueryAsync<dynamic>(session, "Test.SimpleQuery", null);
                var count = result.Count();
                Console.WriteLine($"   ✓ Query executed, got {count} row(s)\n");

                // Test 5: Commit
                Console.WriteLine("5️⃣  Committing transaction...");
                session.Commit();
                Console.WriteLine("   ✓ Transaction committed\n");

                Console.WriteLine("6️⃣  Closing session...");
            }
            Console.WriteLine("   ✓ Session closed\n");

            Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

            // Test error scenario
            Console.WriteLine("7️⃣  Testing error scenario...");
            config.RegisterStatement(new SqlStatement
            {
                Id = "Test.ErrorQuery",
                CommandText = "SELECT * FROM TableDoesNotExist",
                StatementType = SqlStatementType.Select
            });

            try
            {
                using var session2 = sessionFactory.OpenSession();
                await sqlMapper.QueryAsync<dynamic>(session2, "Test.ErrorQuery", null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ✓ Error caught: {ex.Message.Split('\n')[0]}\n");
            }

            // Test warning scenario
            Console.WriteLine("8️⃣  Testing warning scenario...");
            try
            {
                using var session3 = sessionFactory.OpenSession();
                await sqlMapper.QueryAsync<dynamic>(session3, "Does.Not.Exist", null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ✓ Warning logged: {ex.Message}\n");
            }

            Console.WriteLine("═══════════════════════════════════════════════════════════════\n");
            Console.WriteLine("✅ All tests completed!\n");

            var logDate = DateTime.Now.ToString("yyyyMMdd");
            var logDir = Path.Combine(Directory.GetCurrentDirectory(), "log", "iBatis");
            var fullLog = Path.Combine(logDir, $"ibatis-{logDate}.log");
            var errorLog = Path.Combine(logDir, $"ibatis-errors-{logDate}.log");

            Console.WriteLine("📁 Check log files:\n");
            Console.WriteLine($"   Full log:  {fullLog}");
            Console.WriteLine($"   Error log: {errorLog}\n");

            // Display last few lines if file exists
            if (File.Exists(fullLog))
            {
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine("📝 Last 15 lines from log:\n");

                try
                {
                    var lines = File.ReadAllLines(fullLog);
                    var lastLines = lines.TakeLast(15);

                    foreach (var line in lastLines)
                    {
                        // Color code the output
                        if (line.Contains("[ERR]"))
                            Console.ForegroundColor = ConsoleColor.Red;
                        else if (line.Contains("[WRN]"))
                            Console.ForegroundColor = ConsoleColor.Yellow;
                        else if (line.Contains("[INF]"))
                            Console.ForegroundColor = ConsoleColor.Green;
                        else if (line.Contains("[DBG]"))
                            Console.ForegroundColor = ConsoleColor.Cyan;

                        Console.WriteLine(line);
                        Console.ResetColor();
                    }

                    Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
                }
                catch
                {
                    Console.WriteLine("   (Log file might be locked, check manually)");
                }
            }
            else
            {
                Console.WriteLine("⚠️  Log file not found!");
                Console.WriteLine("   Make sure Serilog packages are installed:");
                Console.WriteLine("   - Serilog");
                Console.WriteLine("   - Serilog.Extensions.Logging");
                Console.WriteLine("   - Serilog.Sinks.Console");
                Console.WriteLine("   - Serilog.Sinks.File");
            }
        }

        Console.WriteLine("\n🎯 What to look for in logs:\n");
        Console.WriteLine("   ✓ [DBG] DbSession created - SessionId: xxxxxxxx");
        Console.WriteLine("   ✓ [INF] Connection opened");
        Console.WriteLine("   ✓ [INF] Transaction started");
        Console.WriteLine("   ✓ [DBG] Registered new statement: Test.SimpleQuery");
        Console.WriteLine("   ✓ [DBG] Executing QueryAsync - StatementId: Test.SimpleQuery");
        Console.WriteLine("   ✓ [INF] QueryAsync completed - ResultCount: 1, Duration: XXms");
        Console.WriteLine("   ✓ [INF] Transaction committed successfully");
        Console.WriteLine("   ✓ [INF] Connection closed");
        Console.WriteLine("   ✓ [ERR] QueryAsync failed - StatementId: Test.ErrorQuery");
        Console.WriteLine("   ✓ [WRN] Statement not found: Does.Not.Exist\n");

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
