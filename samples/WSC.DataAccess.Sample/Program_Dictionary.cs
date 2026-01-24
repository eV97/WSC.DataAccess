using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Sample;

namespace WSC.DataAccess.Sample.Examples;

// ============================================================================
// DEMO: Using AddWscDataAccess with Dictionary<string, string>
// ============================================================================
// This approach is useful when:
// - Connection strings come from database/API/environment variables
// - Testing scenarios with programmatic configuration
// - Dynamic configuration at runtime
// - When you need full control over connection string dictionary
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

        // Load configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Convert ConnectionStrings section to Dictionary
        var connectionStrings = new Dictionary<string, string>();
        var connectionStringsSection = configuration.GetSection("ConnectionStrings");
        foreach (var conn in connectionStringsSection.GetChildren())
        {
            var value = conn.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                // Remove "Connection" suffix like the framework does
                var cleanName = conn.Key.EndsWith("Connection", StringComparison.OrdinalIgnoreCase)
                    ? conn.Key.Substring(0, conn.Key.Length - "Connection".Length)
                    : conn.Key;

                connectionStrings[cleanName] = value;
            }
        }

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
