using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Sample.Models;

namespace WSC.DataAccess.Sample
{
    public class Program_Configs
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  WSC.DataAccess - Configs Sample                                ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("This is a placeholder for the Program_Configs sample.");
            Console.WriteLine("Please refer to other Program_*.cs files for actual implementation examples.");
            Console.WriteLine();

            // Configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Display connection strings
            Console.WriteLine("📌 Connection Strings:");
            var connectionStringsSection = configuration.GetSection("ConnectionStrings");
            foreach (var conn in connectionStringsSection.GetChildren())
            {
                var value = conn.Value ?? "";
                var displayValue = value.Length > 50 ? value.Substring(0, 50) + "..." : value;
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

            // WSC.DataAccess with ISql Pattern - Auto-load connection strings from configuration
            services.AddWscDataAccess(configuration, configure: options =>
            {
                // Connection strings are auto-loaded from appsettings.json
                // DefaultConnection -> Default, HISConnection -> HIS, LISConnection -> LIS

                Console.WriteLine("📋 Auto-discovering SQL Map DAOs from 'SqlMaps' directory...");
                options.AutoDiscoverSqlMaps("SqlMaps");

                var daoCount = options.SqlMapProvider.Files.Count;
                Console.WriteLine($"  ✅ {daoCount} DAOs auto-registered");

                // Show registered DAOs
                foreach (var registration in options.SqlMapProvider.Files)
                {
                    var description = Provider.GetDescription(registration.Key);
                    Console.WriteLine($"     - {registration.Key}: {description}");
                }
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
}