using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Constants;
using WSC.DataAccess.RealDB.Test.Repositories;

namespace WSC.DataAccess.RealDB.Test;

/// <summary>
/// ✨ EXAMPLE: Program.cs with Provider Pattern
///
/// Đây là ví dụ thực tế về cách sử dụng Provider Pattern trong Program.cs
/// Giống với pattern trong MrFu.Smartcheck!
/// </summary>
public class ProviderPatternProgramExample
{
    /// <summary>
    /// Example for ASP.NET Core Web API / MVC
    /// </summary>
    public static void WebApplicationExample()
    {
        var builder = WebApplication.CreateBuilder();

        // ═══════════════════════════════════════════════════════════════
        // BƯỚC 1: Cấu hình Logging (Optional)
        // ═══════════════════════════════════════════════════════════════
        builder.Logging.AddIBatisLogging("log/iBatis", LogLevel.Information);

        // ═══════════════════════════════════════════════════════════════
        // BƯỚC 2: Lấy connection string
        // ═══════════════════════════════════════════════════════════════
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");

        // ═══════════════════════════════════════════════════════════════
        // BƯỚC 3: Đăng ký WSC.DataAccess với PROVIDER PATTERN
        // ═══════════════════════════════════════════════════════════════
        builder.Services.AddWscDataAccess(connectionString, options =>
        {
            // ✨ KHAI BÁO SQL MAPS TẬP TRUNG (CENTRALIZED)
            // Giống như MrFu.Smartcheck:
            //   services.AddSmartcheck(opts => opts.ConfigureProviders(...))
            //
            // WSC.DataAccess:
            //   services.AddWscDataAccess(conn, opts => opts.ConfigureSqlMaps(...))

            options.ConfigureSqlMaps(provider =>
            {
                // ───────────────────────────────────────────────────────
                // Order Management Domain
                // ───────────────────────────────────────────────────────
                provider.AddFile(
                    key: "Order",
                    filePath: SqlMapFiles.DAO005,
                    description: "Order management - queries for orders, order items, order status"
                );

                provider.AddFile(
                    key: "OrderItem",
                    filePath: SqlMapFiles.DAO006,
                    description: "Order items and line items"
                );

                // ───────────────────────────────────────────────────────
                // Customer Management Domain
                // ───────────────────────────────────────────────────────
                provider.AddFile(
                    key: "Customer",
                    filePath: SqlMapFiles.DAO010,
                    description: "Customer data, profiles, and preferences"
                );

                provider.AddFile(
                    key: "CustomerAddress",
                    filePath: SqlMapFiles.DAO011,
                    description: "Customer shipping and billing addresses"
                );

                // ───────────────────────────────────────────────────────
                // Product Catalog Domain
                // ───────────────────────────────────────────────────────
                provider.AddFile(
                    key: "Product",
                    filePath: SqlMapFiles.DAO015,
                    description: "Product catalog, specifications, and pricing"
                );

                provider.AddFile(
                    key: "Inventory",
                    filePath: SqlMapFiles.DAO016,
                    description: "Inventory tracking and stock management"
                );

                // ───────────────────────────────────────────────────────
                // Payment & Shipping Domain
                // ───────────────────────────────────────────────────────
                provider.AddFile(
                    key: "Payment",
                    filePath: SqlMapFiles.DAO017,
                    description: "Payment processing and transaction history"
                );

                provider.AddFile(
                    key: "Shipping",
                    filePath: SqlMapFiles.DAO018,
                    description: "Shipping methods, tracking, and delivery"
                );

                // ───────────────────────────────────────────────────────
                // Reporting & Analytics Domain
                // ───────────────────────────────────────────────────────
                provider.AddFile(
                    key: "Report",
                    filePath: SqlMapFiles.DAO020,
                    description: "Business reports and analytics queries"
                );

                // ───────────────────────────────────────────────────────
                // Application & System Domain
                // ───────────────────────────────────────────────────────
                provider.AddFile(
                    key: "Application",
                    filePath: SqlMapFiles.APPLICATION_MAP,
                    description: "Application configuration and system settings"
                );

                provider.AddFile(
                    key: "Generic",
                    filePath: SqlMapFiles.GENERIC_MAP,
                    description: "Generic utilities and common queries"
                );

                // 💡 TIP: Bạn có thể group files theo domain/module
                //         để dễ quản lý trong dự án lớn!
            });

            // Optional: Named connections cho multiple databases
            options.AddConnection("ReportingDB", "Server=...;Database=Reporting;...");
            options.AddConnection("ArchiveDB", "Server=...;Database=Archive;...");
        });

        // ═══════════════════════════════════════════════════════════════
        // BƯỚC 4: Đăng ký Repositories
        // ═══════════════════════════════════════════════════════════════
        // Repositories sẽ tự động inject SqlMapProvider và dùng key!

        builder.Services.AddScoped<ProviderOrderRepository>();
        builder.Services.AddScoped<ProviderCustomerRepository>();

        // Nếu bạn có nhiều repositories:
        // builder.Services.AddScoped<ProductRepository>();
        // builder.Services.AddScoped<InventoryRepository>();
        // builder.Services.AddScoped<PaymentRepository>();
        // builder.Services.AddScoped<ShippingRepository>();
        // builder.Services.AddScoped<ReportRepository>();

        // ═══════════════════════════════════════════════════════════════
        // BƯỚC 5: Đăng ký Services (Business Logic)
        // ═══════════════════════════════════════════════════════════════
        // builder.Services.AddScoped<OrderService>();
        // builder.Services.AddScoped<CustomerService>();
        // builder.Services.AddScoped<ProductService>();

        // ═══════════════════════════════════════════════════════════════
        // BƯỚC 6: Build & Run
        // ═══════════════════════════════════════════════════════════════
        builder.Services.AddControllers();

        var app = builder.Build();

        app.MapControllers();
        app.Run();
    }

    /// <summary>
    /// Example for Console Application
    /// </summary>
    public static void ConsoleApplicationExample()
    {
        var services = new ServiceCollection();

        // ═══════════════════════════════════════════════════════════════
        // BƯỚC 1: Logging
        // ═══════════════════════════════════════════════════════════════
        services.AddLogging(builder =>
        {
            builder.AddIBatisLogging("log/iBatis", LogLevel.Information);
            builder.AddConsole();
        });

        // ═══════════════════════════════════════════════════════════════
        // BƯỚC 2: Connection String
        // ═══════════════════════════════════════════════════════════════
        var connectionString = "Server=localhost;Database=MyDB;User Id=sa;Password=Pass;TrustServerCertificate=True";

        // ═══════════════════════════════════════════════════════════════
        // BƯỚC 3: Provider Pattern Configuration
        // ═══════════════════════════════════════════════════════════════
        services.AddWscDataAccess(connectionString, options =>
        {
            options.ConfigureSqlMaps(provider =>
            {
                // ✨ Khai báo tập trung
                provider.AddFile("Order", SqlMapFiles.DAO005, "Order queries");
                provider.AddFile("Customer", SqlMapFiles.DAO010, "Customer queries");
                provider.AddFile("Product", SqlMapFiles.DAO015, "Product queries");
            });
        });

        // ═══════════════════════════════════════════════════════════════
        // BƯỚC 4: Đăng ký repositories
        // ═══════════════════════════════════════════════════════════════
        services.AddScoped<ProviderOrderRepository>();
        services.AddScoped<ProviderCustomerRepository>();

        // ═══════════════════════════════════════════════════════════════
        // BƯỚC 5: Build & Use
        // ═══════════════════════════════════════════════════════════════
        var serviceProvider = services.BuildServiceProvider();

        // Sử dụng repositories
        using (var scope = serviceProvider.CreateScope())
        {
            var orderRepo = scope.ServiceProvider.GetRequiredService<ProviderOrderRepository>();
            // var orders = await orderRepo.GetAllOrdersAsync();
        }
    }

    /// <summary>
    /// Example for Worker Service / Background Service
    /// </summary>
    public static void WorkerServiceExample()
    {
        var builder = Host.CreateApplicationBuilder();

        // Logging
        builder.Logging.AddIBatisLogging("log/iBatis", LogLevel.Information);

        // Connection
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;

        // Provider Pattern
        builder.Services.AddWscDataAccess(connectionString, options =>
        {
            options.ConfigureSqlMaps(provider =>
            {
                provider.AddFile("Order", SqlMapFiles.DAO005);
                provider.AddFile("Report", SqlMapFiles.DAO020);
            });
        });

        // Repositories
        builder.Services.AddScoped<ProviderOrderRepository>();

        // Background Service
        // builder.Services.AddHostedService<OrderProcessingWorker>();

        var host = builder.Build();
        host.Run();
    }
}

/// <summary>
/// 💡 COMPARISON: Provider Pattern vs Other Patterns
/// </summary>
public class PatternComparison
{
    public void Example1_Hardcoded_BAD()
    {
        // ❌ BAD: File path ở nhiều chỗ, dễ sai
        var services = new ServiceCollection();

        // Repository 1
        // private const string SQL_MAP_FILE = "SqlMaps/DAO005.xml";

        // Repository 2
        // private const string SQL_MAP_FILE = "SqlMaps/DAO010.xml";

        // Repository 3
        // private const string SQL_MAP_FILE = "SqlMaps/DAO015.xml";

        // ❌ VẤN ĐỀ:
        // - File paths scattered across codebase
        // - Hard to maintain
        // - Easy to make mistakes
    }

    public void Example2_Constants_BETTER()
    {
        // ✅ BETTER: Dùng constants
        var services = new ServiceCollection();

        // Repository 1
        // private const string SQL_MAP_FILE = SqlMapFiles.DAO005;

        // Repository 2
        // private const string SQL_MAP_FILE = SqlMapFiles.DAO010;

        // ✅ IMPROVEMENT:
        // - IntelliSense support
        // - Type-safe
        // - Ít lỗi hơn

        // ⚠️ NHƯNG:
        // - Vẫn phải khai báo ở mỗi repository
    }

    public void Example3_Attribute_SIMPLE()
    {
        // ✅ SIMPLE: Dùng attribute
        var services = new ServiceCollection();

        // [SqlMapFile(SqlMapFiles.DAO005)]
        // public class OrderRepository : SimpleSqlMapRepository<Order>

        // ✅ BENEFITS:
        // - Very simple (4 lines)
        // - IntelliSense support
        // - Type-safe

        // ⚠️ NHƯNG:
        // - File path vẫn ở trong repository
        // - Không tập trung
    }

    public void Example4_Provider_BEST()
    {
        // ✅✅✅ BEST: Provider Pattern (TẬP TRUNG)
        var services = new ServiceCollection();

        services.AddWscDataAccess("connection", options =>
        {
            // ✨ Khai báo TẤT CẢ ở MỘT CHỖ!
            options.ConfigureSqlMaps(provider =>
            {
                provider.AddFile("Order", SqlMapFiles.DAO005);
                provider.AddFile("Customer", SqlMapFiles.DAO010);
                provider.AddFile("Product", SqlMapFiles.DAO015);
            });
        });

        // Repository chỉ cần KEY:
        // public class OrderRepository : ProviderBasedRepository<Order>
        // {
        //     private const string MAP_KEY = "Order";
        //     public OrderRepository(IDbSessionFactory sf, SqlMapProvider provider)
        //         : base(sf, provider, MAP_KEY) { }
        // }

        // ✅✅✅ BENEFITS:
        // - CENTRALIZED configuration (giống MrFu.Smartcheck!)
        // - Repository chỉ cần KEY
        // - Dễ quản lý
        // - Dễ thay đổi
        // - IntelliSense support
        // - Type-safe
        // - Enterprise-ready!
    }
}
