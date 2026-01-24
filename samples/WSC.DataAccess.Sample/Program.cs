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
Console.WriteLine($"📌 Connection: {(connectionString != null ? connectionString.Substring(0, Math.Min(50, connectionString.Length)) : "null")}...");
Console.WriteLine();

// DI Setup
var services = new ServiceCollection();
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// WSC.DataAccess with ISql Pattern - Auto-discovery
services.AddWscDataAccess(connectionString!, options =>
{
    Console.WriteLine("📋 Auto-discovering SQL Map DAOs from 'SqlMaps' directory...");

    // Auto-discover tất cả .xml files trong thư mục SqlMaps
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
