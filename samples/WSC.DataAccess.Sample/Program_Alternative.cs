using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Sample;
using WSC.DataAccess.Sample.Models;

namespace WSC.DataAccess.Sample.Alternative;

/// <summary>
/// Alternative Program - Sử dụng Provider.GetDaoFiles() pattern
/// Giống pattern cũ trong MrFu.SmartCheck.Web
/// </summary>
public class ProgramAlternative
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  WSC.DataAccess - Alternative Pattern                           ║");
        Console.WriteLine("║  Using Provider.GetDaoFiles() - Legacy Pattern                  ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        Console.WriteLine($"📌 Connection: {connectionString?.Substring(0, Math.Min(50, connectionString.Length ?? 0))}...");
        Console.WriteLine();

        // DI Setup
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // ============================================================
        // PATTERN 1: Sử dụng Provider.GetDaoFiles() - Giống code cũ
        // ============================================================
        var executingAssemblyLocation = Directory.GetCurrentDirectory();
        var sqlMapsPath = Path.Combine(executingAssemblyLocation, "SqlMaps");

        Console.WriteLine("📋 Getting DAO files from Provider.GetDaoFiles()...");
        var daoFiles = Provider.GetDaoFiles(sqlMapsPath);
        var existingFiles = daoFiles.Where(File.Exists).ToArray();

        Console.WriteLine($"  Found {daoFiles.Length} DAO definitions");
        Console.WriteLine($"  Existing files: {existingFiles.Length}");

        // Register with manual mapping
        services.AddWscDataAccess(connectionString!, options =>
        {
            options.ConfigureSqlMaps(provider =>
            {
                foreach (var file in existingFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var description = Provider.GetDescription(fileName);
                    provider.AddFile(fileName, file, description);
                    Console.WriteLine($"     - {fileName}: {description}");
                }
            });
        });

        services.AddScoped<TestService>();
        Console.WriteLine("✅ DI Container configured\n");

        // Run tests
        var serviceProvider = services.BuildServiceProvider();
        using (var scope = serviceProvider.CreateScope())
        {
            var testService = scope.ServiceProvider.GetRequiredService<TestService>();
            try
            {
                await testService.RunAllTestsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ ERROR: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"   Inner: {ex.InnerException.Message}");
            }
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}

/// <summary>
/// So sánh các patterns:
///
/// PATTERN 1: Auto-Discovery (Recommended - Đơn giản nhất)
/// =========================================================
/// services.AddWscDataAccess(connectionString!, options =>
/// {
///     // Tự động scan thư mục SqlMaps và register tất cả .xml files
///     options.AutoDiscoverSqlMaps("SqlMaps");
/// });
///
///
/// PATTERN 2: Provider.GetDaoFiles() (Legacy - Giống code cũ)
/// =========================================================
/// var daoFiles = Provider.GetDaoFiles("SqlMaps");
/// var existingFiles = daoFiles.Where(File.Exists).ToArray();
///
/// services.AddWscDataAccess(connectionString!, options =>
/// {
///     options.ConfigureSqlMaps(provider =>
///     {
///         foreach (var file in existingFiles)
///         {
///             var fileName = Path.GetFileNameWithoutExtension(file);
///             provider.AddFile(fileName, file);
///         }
///     });
/// });
///
///
/// PATTERN 3: Manual Registration (Full Control)
/// =========================================================
/// services.AddWscDataAccess(connectionString!, options =>
/// {
///     options.ConfigureSqlMaps(provider =>
///     {
///         provider.AddFile(Provider.DAO000, "SqlMaps/DAO000.xml", "System");
///         provider.AddFile(Provider.DAO001, "SqlMaps/DAO001.xml", "User");
///         // ...
///     });
/// });
///
///
/// Recommendation:
/// - Dùng PATTERN 1 (Auto-Discovery) cho hầu hết trường hợp
/// - Dùng PATTERN 2 nếu cần kiểm tra file existence trước khi register
/// - Dùng PATTERN 3 nếu cần full control hoặc conditional registration
/// </summary>
