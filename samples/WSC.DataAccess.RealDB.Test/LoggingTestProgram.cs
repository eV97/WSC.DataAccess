using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Core;
using WSC.DataAccess.Mapping;

namespace WSC.DataAccess.RealDB.Test;

/// <summary>
/// Program để test iBatis logging - Tạo các scenarios để xem logs
/// </summary>
public class LoggingTestProgram
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  iBatis Logging Test Cases                                      ║");
        Console.WriteLine("║  Kiểm tra logs được ghi vào log/iBatis/                         ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");

        // Setup DI với logging
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddIBatisLogging(minimumLevel: Serilog.Events.LogEventLevel.Debug);

                Console.WriteLine("✓ iBatis logging configured (Debug level)");
                Console.WriteLine($"   Log directory: log/iBatis/");
                Console.WriteLine($"   Files: ibatis-{DateTime.Now:yyyyMMdd}.log, ibatis-errors-{DateTime.Now:yyyyMMdd}.log");
                Console.WriteLine();
            })
            .ConfigureServices((context, services) =>
            {
                services.AddWscDataAccess(connectionString, options =>
                {
                    var sqlMapPath = Path.Combine(AppContext.BaseDirectory, "SqlMaps", "GenericMap.xml");
                    if (File.Exists(sqlMapPath))
                    {
                        options.AddSqlMapFile(sqlMapPath);
                    }
                });
            })
            .Build();

        using (var scope = host.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            try
            {
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine("  TEST CASES - Check log files for details");
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine();

                // Test 1: SQL Map Loading
                await TestCase1_SqlMapLoading(services);
                await Task.Delay(500);

                // Test 2: Successful Query
                await TestCase2_SuccessfulQuery(services);
                await Task.Delay(500);

                // Test 3: Failed Query (SQL Error)
                await TestCase3_FailedQuery(services);
                await Task.Delay(500);

                // Test 4: Statement Not Found
                await TestCase4_StatementNotFound(services);
                await Task.Delay(500);

                // Test 5: Transaction Commit
                await TestCase5_TransactionCommit(services);
                await Task.Delay(500);

                // Test 6: Transaction Rollback
                await TestCase6_TransactionRollback(services);
                await Task.Delay(500);

                // Test 7: Connection Lifecycle
                await TestCase7_ConnectionLifecycle(services);
                await Task.Delay(500);

                // Test 8: Multiple Sessions
                await TestCase8_MultipleSessions(services);
                await Task.Delay(500);

                Console.WriteLine();
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine("  ✅ ALL TEST CASES COMPLETED");
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine();
                Console.WriteLine("📁 Check log files:");
                Console.WriteLine($"   - log/iBatis/ibatis-{DateTime.Now:yyyyMMdd}.log");
                Console.WriteLine($"   - log/iBatis/ibatis-errors-{DateTime.Now:yyyyMMdd}.log");
                Console.WriteLine();
                Console.WriteLine("🔍 What to look for in logs:");
                Console.WriteLine("   ✓ [INF] SQL map loading messages");
                Console.WriteLine("   ✓ [DBG] Query execution start messages");
                Console.WriteLine("   ✓ [INF] Query completion with duration");
                Console.WriteLine("   ✓ [ERR] Error messages with stack traces");
                Console.WriteLine("   ✓ [WRN] Warning messages (statement not found, rollback)");
                Console.WriteLine("   ✓ [INF] Connection open/close messages");
                Console.WriteLine("   ✓ [INF] Transaction commit/rollback messages");
                Console.WriteLine("   ✓ Session IDs tracking operations");
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("❌ UNEXPECTED ERROR:");
                Console.WriteLine($"   {ex.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    static async Task TestCase1_SqlMapLoading(IServiceProvider services)
    {
        Console.WriteLine("Test 1: SQL Map Loading");
        Console.WriteLine("   Expected logs: [INF] Loading SQL map, [DBG] Loaded statements");
        Console.WriteLine("   ✓ SQL map config already loaded during DI setup");
        Console.WriteLine();
    }

    static async Task TestCase2_SuccessfulQuery(IServiceProvider services)
    {
        Console.WriteLine("Test 2: Successful Query Execution");
        Console.WriteLine("   Expected logs: [DBG] Executing QueryAsync, [INF] QueryAsync completed with duration");

        try
        {
            var sessionFactory = services.GetRequiredService<IDbSessionFactory>();
            var sqlMapper = services.GetRequiredService<SqlMapper>();

            using var session = sessionFactory.OpenSession();

            // Query generic data
            var result = await sqlMapper.QueryAsync<dynamic>(
                session,
                "Generic.GetTableNames",
                null);

            Console.WriteLine($"   ✓ Query executed successfully, found {result.Count()} records");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️  Query failed (expected if statement doesn't exist): {ex.Message}");
        }

        Console.WriteLine();
    }

    static async Task TestCase3_FailedQuery(IServiceProvider services)
    {
        Console.WriteLine("Test 3: Failed Query (SQL Error)");
        Console.WriteLine("   Expected logs: [ERR] QueryAsync failed with exception");

        try
        {
            var sessionFactory = services.GetRequiredService<IDbSessionFactory>();
            var config = services.GetRequiredService<SqlMapConfig>();

            // Register a statement with invalid SQL
            config.RegisterStatement(new SqlStatement
            {
                Id = "Test.InvalidQuery",
                CommandText = "SELECT * FROM NonExistentTable WHERE Invalid = 1",
                StatementType = SqlStatementType.Select
            });

            var sqlMapper = services.GetRequiredService<SqlMapper>();
            using var session = sessionFactory.OpenSession();

            var result = await sqlMapper.QueryAsync<dynamic>(
                session,
                "Test.InvalidQuery",
                null);

            Console.WriteLine("   ❌ Should have thrown exception!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✓ Exception caught as expected: {ex.Message.Split('\n')[0]}");
        }

        Console.WriteLine();
    }

    static async Task TestCase4_StatementNotFound(IServiceProvider services)
    {
        Console.WriteLine("Test 4: Statement Not Found");
        Console.WriteLine("   Expected logs: [WRN] Statement not found");

        try
        {
            var sessionFactory = services.GetRequiredService<IDbSessionFactory>();
            var sqlMapper = services.GetRequiredService<SqlMapper>();

            using var session = sessionFactory.OpenSession();

            var result = await sqlMapper.QueryAsync<dynamic>(
                session,
                "NonExistent.Statement",
                null);

            Console.WriteLine("   ❌ Should have thrown exception!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✓ Exception caught: {ex.Message}");
        }

        Console.WriteLine();
    }

    static async Task TestCase5_TransactionCommit(IServiceProvider services)
    {
        Console.WriteLine("Test 5: Transaction Commit");
        Console.WriteLine("   Expected logs: [INF] Transaction started, [INF] Transaction committed");

        try
        {
            var sessionFactory = services.GetRequiredService<IDbSessionFactory>();

            using (var session = sessionFactory.OpenSession())
            {
                session.BeginTransaction();

                // Simulate some work
                await Task.Delay(100);

                session.Commit();
                Console.WriteLine("   ✓ Transaction committed successfully");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️  Transaction failed: {ex.Message}");
        }

        Console.WriteLine();
    }

    static async Task TestCase6_TransactionRollback(IServiceProvider services)
    {
        Console.WriteLine("Test 6: Transaction Rollback");
        Console.WriteLine("   Expected logs: [INF] Transaction started, [WRN] Transaction rolled back");

        try
        {
            var sessionFactory = services.GetRequiredService<IDbSessionFactory>();

            using (var session = sessionFactory.OpenSession())
            {
                session.BeginTransaction();

                // Simulate some work
                await Task.Delay(100);

                session.Rollback();
                Console.WriteLine("   ✓ Transaction rolled back successfully");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️  Rollback failed: {ex.Message}");
        }

        Console.WriteLine();
    }

    static async Task TestCase7_ConnectionLifecycle(IServiceProvider services)
    {
        Console.WriteLine("Test 7: Connection Lifecycle");
        Console.WriteLine("   Expected logs: [INF] Connection opened, [INF] Connection closed");

        try
        {
            var sessionFactory = services.GetRequiredService<IDbSessionFactory>();

            // Open and immediately close
            using (var session = sessionFactory.OpenSession())
            {
                await Task.Delay(100);
                // Session will be disposed here
            }

            Console.WriteLine("   ✓ Connection lifecycle completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️  Connection test failed: {ex.Message}");
        }

        Console.WriteLine();
    }

    static async Task TestCase8_MultipleSessions(IServiceProvider services)
    {
        Console.WriteLine("Test 8: Multiple Concurrent Sessions");
        Console.WriteLine("   Expected logs: Multiple session IDs with different operations");

        try
        {
            var sessionFactory = services.GetRequiredService<IDbSessionFactory>();

            // Create 3 sessions concurrently
            var tasks = new List<Task>();

            for (int i = 1; i <= 3; i++)
            {
                int sessionNum = i;
                tasks.Add(Task.Run(async () =>
                {
                    using var session = sessionFactory.OpenSession();
                    await Task.Delay(100 * sessionNum);
                }));
            }

            await Task.WhenAll(tasks);

            Console.WriteLine("   ✓ Multiple sessions completed (check logs for different SessionIds)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️  Multiple sessions test failed: {ex.Message}");
        }

        Console.WriteLine();
    }
}
