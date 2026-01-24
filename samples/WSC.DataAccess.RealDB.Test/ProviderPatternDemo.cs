using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Constants;
using WSC.DataAccess.RealDB.Test.Repositories;

namespace WSC.DataAccess.RealDB.Test;

/// <summary>
/// ✨ DEMO: Provider Pattern - Giống MrFu.Smartcheck!
///
/// Khai báo tất cả SQL maps TẬP TRUNG ở Program.cs,
/// Repositories chỉ cần dùng KEY!
/// </summary>
public class ProviderPatternDemo
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("✨ PROVIDER PATTERN DEMO");
        Console.WriteLine("Giống MrFu.Smartcheck Provider Pattern!");
        Console.WriteLine("========================================\n");

        // Bước 1: Cấu hình DI Container
        var services = new ServiceCollection();

        // Bước 2: Cấu hình logging
        services.AddLogging(builder =>
        {
            builder.AddIBatisLogging("log/iBatis", LogLevel.Information);
        });

        // Bước 3: Cấu hình WSC DataAccess với PROVIDER PATTERN
        var connectionString = "Server=localhost;Database=WSC;User Id=sa;Password=YourPassword;TrustServerCertificate=True";

        services.AddWscDataAccess(connectionString, options =>
        {
            // ✨ MAGIC HERE! Khai báo SQL maps như PROVIDER
            options.ConfigureSqlMaps(provider =>
            {
                // Khai báo tất cả SQL map files TẬP TRUNG ở đây!
                provider.AddFile("Order", SqlMapFiles.DAO005, "Order management and processing");
                provider.AddFile("Customer", SqlMapFiles.DAO010, "Customer data and profiles");
                provider.AddFile("Product", SqlMapFiles.DAO015, "Product catalog management");
                provider.AddFile("Inventory", SqlMapFiles.DAO016, "Inventory tracking");
                provider.AddFile("Report", SqlMapFiles.DAO020, "Reporting and analytics");

                Console.WriteLine("✅ Registered SQL Maps:");
                Console.WriteLine("   - Order     -> DAO005.xml");
                Console.WriteLine("   - Customer  -> DAO010.xml");
                Console.WriteLine("   - Product   -> DAO015.xml");
                Console.WriteLine("   - Inventory -> DAO016.xml");
                Console.WriteLine("   - Report    -> DAO020.xml");
                Console.WriteLine();
            });
        });

        // Bước 4: Đăng ký repositories
        services.AddScoped<ProviderOrderRepository>();
        services.AddScoped<ProviderCustomerRepository>();

        // Bước 5: Build service provider
        var serviceProvider = services.BuildServiceProvider();

        Console.WriteLine("========================================");
        Console.WriteLine("📋 TESTING PROVIDER PATTERN");
        Console.WriteLine("========================================\n");

        try
        {
            // Test 1: Order Repository
            Console.WriteLine("Test 1: Order Repository (uses 'Order' key)");
            Console.WriteLine("--------------------------------------------");
            var orderRepo = serviceProvider.GetRequiredService<ProviderOrderRepository>();
            var orders = await orderRepo.GetAllOrdersAsync();
            Console.WriteLine($"✅ Retrieved {orders.Count()} orders from DAO005.xml");
            Console.WriteLine($"   File: {SqlMapFiles.DAO005}");
            Console.WriteLine($"   Key:  'Order'");
            Console.WriteLine();

            // Test 2: Customer Repository
            Console.WriteLine("Test 2: Customer Repository (uses 'Customer' key)");
            Console.WriteLine("--------------------------------------------");
            var customerRepo = serviceProvider.GetRequiredService<ProviderCustomerRepository>();
            var customers = await customerRepo.GetAllCustomersAsync();
            Console.WriteLine($"✅ Retrieved {customers.Count()} customers from DAO010.xml");
            Console.WriteLine($"   File: {SqlMapFiles.DAO010}");
            Console.WriteLine($"   Key:  'Customer'");
            Console.WriteLine();

            // Test 3: Kiểm tra provider
            Console.WriteLine("Test 3: Provider Information");
            Console.WriteLine("--------------------------------------------");
            var provider = serviceProvider.GetRequiredService<SqlMapProvider>();
            Console.WriteLine($"✅ Total registered maps: {provider.Files.Count}");
            foreach (var file in provider.Files)
            {
                Console.WriteLine($"   - Key: '{file.Key}' -> {file.FilePath}");
                Console.WriteLine($"     Description: {file.Description ?? "N/A"}");
            }
            Console.WriteLine();

            Console.WriteLine("========================================");
            Console.WriteLine("✅ ALL TESTS PASSED!");
            Console.WriteLine("========================================\n");

            Console.WriteLine("💡 Provider Pattern Benefits:");
            Console.WriteLine("   ✅ Centralized configuration (like MrFu.Smartcheck)");
            Console.WriteLine("   ✅ Repositories only need KEY, not file path");
            Console.WriteLine("   ✅ Easy to manage and change");
            Console.WriteLine("   ✅ Type-safe with SqlMapFiles constants");
            Console.WriteLine("   ✅ IntelliSense support");
            Console.WriteLine();

            Console.WriteLine("📝 Check logs at: log/iBatis/");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR: {ex.Message}");
            Console.WriteLine($"   Details: {ex}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
