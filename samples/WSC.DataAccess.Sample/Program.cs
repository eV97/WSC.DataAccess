using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Sample;
using WSC.DataAccess.Sample.Models;

Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║  WSC.DataAccess - ConfigurationSection Demo                     ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
Console.WriteLine();

// Configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Get ConnectionStrings section
var connectionStringsSection = configuration.GetSection("ConnectionStrings");

Console.WriteLine("📌 Connection Strings from Section:");
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

// Use the IConfigurationSection overload
services.AddWscDataAccess(connectionStringsSection, configure: options =>
{
    // Connection strings are auto-loaded from section
    // DefaultConnection -> Default, HISConnection -> HIS, LISConnection -> LIS

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
