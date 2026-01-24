using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Sample;
using WSC.DataAccess.Sample.Models;

Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║  WSC.DataAccess - ISql Pattern Test                             ║");
Console.WriteLine("║  Real Database Connection Test                                   ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
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
services.AddWscDataAccess(configuration, options =>
{
    Console.WriteLine("📋 Registering Connection Strings:");

    // Register additional named connections
    if (!string.IsNullOrEmpty(hisConnection))
    {
        options.AddConnection("HIS", hisConnection);
        Console.WriteLine("  ✅ HIS Connection registered");
    }

    if (!string.IsNullOrEmpty(lisConnection))
    {
        options.AddConnection("LIS", lisConnection);
        Console.WriteLine("  ✅ LIS Connection registered");
    }

    Console.WriteLine();
    Console.WriteLine("📋 Auto-discovering SQL Map DAOs from 'SqlMaps' directory...");

    // Auto-discover SQL maps for Default connection
    options.AutoDiscoverSqlMaps("SqlMaps");

    // Hoặc có thể chỉ định connection cụ thể cho từng thư mục:
    // options.AutoDiscoverSqlMaps("SqlMaps/HIS", "HIS");
    // options.AutoDiscoverSqlMaps("SqlMaps/LIS", "LIS");

    var daoCount = options.SqlMapProvider.Files.Count;
    Console.WriteLine($"  ✅ {daoCount} DAOs auto-registered");

    // Show registered DAOs
    foreach (var registration in options.SqlMapProvider.Files)
    {
        var description = Provider.GetDescription(registration.Key);
        Console.WriteLine($"     - {registration.Key} ({registration.ConnectionName}): {description}");
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
