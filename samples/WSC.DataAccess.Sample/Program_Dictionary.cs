using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Sample;

namespace WSC.DataAccess.Sample.Examples;

// ============================================================================
// DEMO: Using AddWscDataAccess with Dictionary<string, string>
// ============================================================================
// This approach is useful when:
// - Not using appsettings.json
// - Connection strings come from database/API/environment variables
// - Testing scenarios
// - Dynamic configuration
// ============================================================================
// To run this demo, change the startup object in project settings or
// comment out the main Program.cs
// ============================================================================

public class Program_Dictionary
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  WSC.DataAccess - Dictionary Connection Strings Demo            ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Define connection strings in a dictionary
        var connectionStrings = new Dictionary<string, string>
        {
            ["Default"] = "Server=FHC-VUONGLH3\\SQLEXPRESS02;Database=LP_ApplicationSystem;User Id=admin;Password=admin;TrustServerCertificate=True;MultipleActiveResultSets=true",
            ["HIS"] = "Server=FHC-VUONGLH3\\SQLEXPRESS02;Database=LP_HIS;User Id=admin;Password=admin;TrustServerCertificate=True;MultipleActiveResultSets=true",
            ["LIS"] = "Server=FHC-VUONGLH3\\SQLEXPRESS02;Database=LP_LIS;User Id=admin;Password=admin;TrustServerCertificate=True;MultipleActiveResultSets=true"
        };

        Console.WriteLine("📌 Connection Strings:");
        foreach (var conn in connectionStrings)
        {
            var displayValue = conn.Value.Length > 50 ? conn.Value.Substring(0, 50) + "..." : conn.Value;
            Console.WriteLine($"   - {conn.Key}: {displayValue}");
        }
        Console.WriteLine();

        // DI Setup
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Use the Dictionary overload
        services.AddWscDataAccess(connectionStrings, defaultConnectionName: "Default", configure: options =>
        {
            Console.WriteLine("📋 Auto-discovering SQL Map DAOs from 'SqlMaps' directory...");
            options.AutoDiscoverSqlMaps("SqlMaps");

            var daoCount = options.SqlMapProvider.Files.Count;
            Console.WriteLine($"  ✅ {daoCount} DAOs auto-registered");
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
