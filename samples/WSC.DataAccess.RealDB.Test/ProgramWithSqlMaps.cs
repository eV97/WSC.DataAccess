using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Core;
using WSC.DataAccess.RealDB.Test.Repositories;
using WSC.DataAccess.RealDB.Test.Models;
using Dapper;

namespace WSC.DataAccess.RealDB.Test;

/// <summary>
/// Program demo sử dụng SqlMaps (IBatis-style)
/// Chạy file này để test SQL Mapping với database thực tế
/// </summary>
public class ProgramWithSqlMaps
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  WSC.DataAccess - SqlMaps (IBatis-style) Demo                   ║");
        Console.WriteLine("║  Database: LP_ApplicationSystem                                  ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        // Get connection string
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");

        Console.WriteLine("📋 Configuration:");
        Console.WriteLine($"   Server: FHC-VUONGLH3\\SQLEXPRESS02");
        Console.WriteLine($"   Database: LP_ApplicationSystem");
        Console.WriteLine($"   User: admin");
        Console.WriteLine($"   SQL Maps: SqlMaps/ApplicationMap.xml");
        Console.WriteLine();

        // Setup DI với SqlMaps
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging((context, logging) =>
            {
                // Configure iBatis logging
                logging.ClearProviders();
                logging.AddIBatisLogging();

                Console.WriteLine("✓ iBatis logging configured");
                Console.WriteLine($"   Log directory: log/iBatis/");
                Console.WriteLine($"   Log files: ibatis-YYYYMMDD.log, ibatis-errors-YYYYMMDD.log");
                Console.WriteLine();
            })
            .ConfigureServices((context, services) =>
            {
                // 🔥 QUAN TRỌNG: Đăng ký SQL Map files
                services.AddWscDataAccess(connectionString, options =>
                {
                    // Đăng ký SQL Map XML files
                    var sqlMapPath1 = Path.Combine(AppContext.BaseDirectory, "SqlMaps", "ApplicationMap.xml");
                    var sqlMapPath2 = Path.Combine(AppContext.BaseDirectory, "SqlMaps", "GenericMap.xml");

                    if (File.Exists(sqlMapPath1))
                    {
                        Console.WriteLine($"✓ Loading SQL Map: {sqlMapPath1}");
                        options.AddSqlMapFile(sqlMapPath1);
                    }
                    else
                    {
                        Console.WriteLine($"⚠️  SQL Map not found: {sqlMapPath1}");
                    }

                    if (File.Exists(sqlMapPath2))
                    {
                        Console.WriteLine($"✓ Loading SQL Map: {sqlMapPath2}");
                        options.AddSqlMapFile(sqlMapPath2);
                    }
                });

                // Đăng ký Repository sử dụng SqlMaps
                services.AddScoped<ApplicationRepository>();
            })
            .Build();

        Console.WriteLine();

        using (var scope = host.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            try
            {
                // =================================================================
                // DEMO 1: Sử dụng SqlMapRepository
                // =================================================================
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine("  DEMO 1: SqlMapRepository Pattern (IBatis-style)");
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine();

                var appRepo = services.GetRequiredService<ApplicationRepository>();

                // Test 1.1: GetAll từ XML
                Console.WriteLine("📝 Test 1.1: GetAll Applications (từ ApplicationMap.xml)");
                Console.WriteLine("   SQL Statement ID: Application.GetAll");
                try
                {
                    var apps = await appRepo.GetAllApplicationsAsync();
                    var appsList = apps.ToList();

                    Console.WriteLine($"   ✓ Found {appsList.Count} applications");

                    if (appsList.Any())
                    {
                        Console.WriteLine("   Sample data:");
                        foreach (var app in appsList.Take(3))
                        {
                            Console.WriteLine($"      - {app.ApplicationName} (v{app.Version ?? "N/A"})");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠️  Error: {ex.Message}");
                    Console.WriteLine("   Tip: Kiểm tra table name và column names trong ApplicationMap.xml");
                }
                Console.WriteLine();

                // Test 1.2: GetById từ XML
                Console.WriteLine("📝 Test 1.2: Get Application By ID (từ ApplicationMap.xml)");
                Console.WriteLine("   SQL Statement ID: Application.GetById");
                try
                {
                    var app = await appRepo.GetByIdAsync(1);
                    if (app != null)
                    {
                        Console.WriteLine($"   ✓ Found: {app.ApplicationName}");
                        Console.WriteLine($"      Description: {app.Description ?? "N/A"}");
                        Console.WriteLine($"      Version: {app.Version ?? "N/A"}");
                    }
                    else
                    {
                        Console.WriteLine("   ⚠️  Application with ID 1 not found");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠️  Error: {ex.Message}");
                }
                Console.WriteLine();

                // Test 1.3: Search từ XML
                Console.WriteLine("📝 Test 1.3: Search Applications (từ ApplicationMap.xml)");
                Console.WriteLine("   SQL Statement ID: Application.SearchByName");
                try
                {
                    var searchResults = await appRepo.SearchByNameAsync("app");
                    Console.WriteLine($"   ✓ Found {searchResults.Count()} applications matching 'app'");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠️  Error: {ex.Message}");
                }
                Console.WriteLine();

                // Test 1.4: Count từ XML
                Console.WriteLine("📝 Test 1.4: Count Active Applications (từ ApplicationMap.xml)");
                Console.WriteLine("   SQL Statement ID: Application.GetActiveCount");
                try
                {
                    var count = await appRepo.GetActiveCountAsync();
                    Console.WriteLine($"   ✓ Active applications: {count}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠️  Error: {ex.Message}");
                }
                Console.WriteLine();

                // =================================================================
                // DEMO 2: Direct SqlMapper Usage
                // =================================================================
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine("  DEMO 2: Direct SqlMapper Usage");
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine();

                var sessionFactory = services.GetRequiredService<IDbSessionFactory>();
                var sqlMapper = services.GetRequiredService<WSC.DataAccess.Mapping.SqlMapper>();

                // Test 2.1: Query với SqlMapper trực tiếp
                Console.WriteLine("📝 Test 2.1: Direct SqlMapper Query");
                try
                {
                    using var session = sessionFactory.OpenSession();

                    // Sử dụng SqlMapper trực tiếp
                    var results = await sqlMapper.QueryAsync<Application>(
                        session,
                        "Application.GetAll",
                        null);

                    Console.WriteLine($"   ✓ Retrieved {results.Count()} records using SqlMapper directly");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠️  Error: {ex.Message}");
                }
                Console.WriteLine();

                // =================================================================
                // DEMO 3: Transaction với SqlMaps
                // =================================================================
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine("  DEMO 3: Transaction với SqlMaps");
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine();

                Console.WriteLine("📝 Test 3.1: Insert và Update trong Transaction");
                try
                {
                    // Sử dụng session factory để tạo transaction thủ công
                    using (var session = sessionFactory.OpenSession())
                    {
                        session.BeginTransaction();

                        try
                        {
                            // Insert new application
                            var newApp = new Application
                            {
                                ApplicationName = $"Test App {DateTime.Now.Ticks}",
                                Description = "Created by SqlMaps demo",
                                Version = "1.0.0"
                            };

                            Console.WriteLine($"   → Inserting: {newApp.ApplicationName}");
                            await sqlMapper.ExecuteAsync(session, "Application.Insert", newApp);

                            Console.WriteLine("   → Transaction committed successfully");
                            session.Commit();

                            Console.WriteLine("   ✓ Transaction demo completed");
                        }
                        catch (Exception ex)
                        {
                            session.Rollback();
                            Console.WriteLine($"   ⚠️  Transaction rolled back: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠️  Error: {ex.Message}");
                    Console.WriteLine("   Note: This is expected if table structure doesn't match");
                }
                Console.WriteLine();

                // =================================================================
                // DEMO 4: Comparison - XML vs Code
                // =================================================================
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine("  DEMO 4: So sánh SQL trong XML vs SQL trong Code");
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine();

                Console.WriteLine("📝 Cách 1: SQL trong XML (IBatis-style)");
                Console.WriteLine("   File: SqlMaps/ApplicationMap.xml");
                Console.WriteLine(@"   <select id=""Application.GetAll"">
       SELECT * FROM Applications WHERE IsActive = 1
   </select>");
                Console.WriteLine();
                Console.WriteLine("   Code:");
                Console.WriteLine("   var apps = await appRepo.GetAllApplicationsAsync();");
                Console.WriteLine("   → Ưu điểm: SQL tập trung, dễ maintain, DBA có thể review");
                Console.WriteLine();

                Console.WriteLine("📝 Cách 2: SQL trong Code (Dapper)");
                Console.WriteLine("   Code:");
                Console.WriteLine(@"   var sql = ""SELECT * FROM Applications WHERE IsActive = 1"";");
                Console.WriteLine("   var apps = await connection.QueryAsync<Application>(sql);");
                Console.WriteLine("   → Ưu điểm: Đơn giản, nhanh, phù hợp queries đơn giản");
                Console.WriteLine();

                // =================================================================
                // SUMMARY
                // =================================================================
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine("  📚 SUMMARY - Cách sử dụng SqlMaps");
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine();

                Console.WriteLine("✅ Các bước sử dụng SqlMaps:");
                Console.WriteLine();
                Console.WriteLine("1️⃣  Tạo SQL Map XML file (SqlMaps/YourEntityMap.xml)");
                Console.WriteLine("   - Định nghĩa các statements: <select>, <insert>, <update>, <delete>");
                Console.WriteLine("   - Mỗi statement có ID unique: Entity.Action");
                Console.WriteLine();

                Console.WriteLine("2️⃣  Đăng ký SQL Map trong Program.cs");
                Console.WriteLine("   services.AddWscDataAccess(connectionString, options => {");
                Console.WriteLine("       options.AddSqlMapFile(\"SqlMaps/YourEntityMap.xml\");");
                Console.WriteLine("   });");
                Console.WriteLine();

                Console.WriteLine("3️⃣  Tạo Repository extends SqlMapRepository<T>");
                Console.WriteLine("   public class YourRepository : SqlMapRepository<YourEntity>");
                Console.WriteLine("   {");
                Console.WriteLine("       public async Task<IEnumerable<YourEntity>> GetAllAsync()");
                Console.WriteLine("       {");
                Console.WriteLine("           return await QueryListAsync(\"YourEntity.GetAll\");");
                Console.WriteLine("       }");
                Console.WriteLine("   }");
                Console.WriteLine();

                Console.WriteLine("4️⃣  Sử dụng Repository");
                Console.WriteLine("   var repo = services.GetRequiredService<YourRepository>();");
                Console.WriteLine("   var data = await repo.GetAllAsync();");
                Console.WriteLine();

                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine("  ✅ DEMO COMPLETED!");
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║  ❌ ERROR OCCURRED                                                ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
                Console.WriteLine();
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Common Issues:");
                Console.WriteLine("1. Table name không đúng trong XML");
                Console.WriteLine("2. Column names không match với database");
                Console.WriteLine("3. Model class properties không match với SQL columns");
                Console.WriteLine("4. SQL Map file không được load (check file path)");
                Console.WriteLine();
                Console.WriteLine("Solution:");
                Console.WriteLine("1. Chạy Program.cs trước để xem table structure");
                Console.WriteLine("2. Update ApplicationMap.xml với đúng table/column names");
                Console.WriteLine("3. Update Models/Application.cs cho phù hợp");
                Console.WriteLine();
                Console.WriteLine("Stack Trace:");
                Console.WriteLine(ex.StackTrace);
            }
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
