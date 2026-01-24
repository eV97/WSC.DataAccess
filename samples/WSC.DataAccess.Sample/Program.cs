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

// WSC.DataAccess with ISql Pattern
services.AddWscDataAccess(connectionString!, options =>
{
    Console.WriteLine("📋 Registering SQL Map DAOs:");
    options.ConfigureSqlMaps(provider =>
    {
        provider.AddFile(Provider.DAO000, "SqlMaps/DAO000.xml", "System");
        provider.AddFile(Provider.DAO001, "SqlMaps/DAO001.xml", "User");
        provider.AddFile(Provider.DAO002, "SqlMaps/DAO002.xml", "Product");
        provider.AddFile(Provider.DAO003, "SqlMaps/DAO003.xml", "Order");
        provider.AddFile(Provider.DAO004, "SqlMaps/DAO004.xml", "Category");
        provider.AddFile(Provider.DAO005, "SqlMaps/DAO005.xml", "Reports");
        Console.WriteLine("  ✅ 6 DAOs registered");
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
