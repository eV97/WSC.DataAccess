using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Sample.Models;
using WSC.DataAccess.Sample.Services;

namespace WSC.DataAccess.Sample;

/// <summary>
/// ✨ Demo: DaoNames Pattern - Project tự định nghĩa DaoNames
///
/// Demonstrates:
/// - Project-defined DaoNames (Models/Provider.cs)
/// - Mapping DaoNames to WSC.DataAccess
/// - Single DAO service (SimpleUserService - 1 DAO)
/// - Multiple DAO service (ComplexBusinessService - nhiều DAOs)
/// - Provider pattern với IntelliSense support
/// </summary>
public class ProviderPatternDemo
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  WSC.DataAccess - Provider Pattern Demo                         ║");
        Console.WriteLine("║  Project tự định nghĩa DaoNames                                  ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // ═══════════════════════════════════════════════════════════════
        // BƯỚC 1: Show Provider Class (Project tự định nghĩa)
        // ═══════════════════════════════════════════════════════════════

        Console.WriteLine("BƯỚC 1: Project Sử dụng - Khai báo DaoNames");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("📁 File: Models/Provider.cs");
        Console.WriteLine("--------------------------------------------");
        Console.WriteLine("public static class Provider");
        Console.WriteLine("{");
        Console.WriteLine("    public static readonly string DAO000 = \"DAO000\"; // System");
        Console.WriteLine("    public static readonly string DAO001 = \"DAO001\"; // User");
        Console.WriteLine("    public static readonly string DAO002 = \"DAO002\"; // Product");
        Console.WriteLine("    public static readonly string DAO003 = \"DAO003\"; // Order");
        Console.WriteLine("    public static readonly string DAO004 = \"DAO004\"; // Category");
        Console.WriteLine("    public static readonly string DAO005 = \"DAO005\"; // Reports");
        Console.WriteLine("}");
        Console.WriteLine();

        Console.WriteLine("📋 Registered DaoNames:");
        Console.WriteLine("--------------------------------------------");
        var allDaoNames = Provider.GetAllDaoNames();
        foreach (var daoName in allDaoNames)
        {
            var description = Provider.GetDescription(daoName);
            Console.WriteLine($"  ✅ {daoName} - {description}");
        }
        Console.WriteLine();

        // ═══════════════════════════════════════════════════════════════
        // BƯỚC 2: Setup DI Container với Provider Pattern
        // ═══════════════════════════════════════════════════════════════

        Console.WriteLine("BƯỚC 2: Program.cs - Ánh xạ DaoNames → Files");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine();

        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Connection string
        var connectionString = "Server=localhost;Database=TestDB;User Id=sa;Password=YourPassword;TrustServerCertificate=True";

        // ✨ WSC DataAccess with Provider Pattern
        Console.WriteLine("services.AddWscDataAccess(connectionString, options =>");
        Console.WriteLine("{");
        Console.WriteLine("    options.ConfigureSqlMaps(provider =>");
        Console.WriteLine("    {");

        services.AddWscDataAccess(connectionString, options =>
        {
            options.ConfigureSqlMaps(provider =>
            {
                // ✅ Ánh xạ từ DaoNames (Provider) → File paths
                provider.AddFile(Provider.DAO000, "SqlMaps/DAO000.xml", "System & Configuration");
                Console.WriteLine($"        provider.AddFile(Provider.{nameof(Provider.DAO000)}, \"SqlMaps/DAO000.xml\");");

                provider.AddFile(Provider.DAO001, "SqlMaps/DAO001.xml", "User Management");
                Console.WriteLine($"        provider.AddFile(Provider.{nameof(Provider.DAO001)}, \"SqlMaps/DAO001.xml\");");

                provider.AddFile(Provider.DAO002, "SqlMaps/DAO002.xml", "Product Management");
                Console.WriteLine($"        provider.AddFile(Provider.{nameof(Provider.DAO002)}, \"SqlMaps/DAO002.xml\");");

                provider.AddFile(Provider.DAO003, "SqlMaps/DAO003.xml", "Order Management");
                Console.WriteLine($"        provider.AddFile(Provider.{nameof(Provider.DAO003)}, \"SqlMaps/DAO003.xml\");");

                provider.AddFile(Provider.DAO004, "SqlMaps/DAO004.xml", "Category Management");
                Console.WriteLine($"        provider.AddFile(Provider.{nameof(Provider.DAO004)}, \"SqlMaps/DAO004.xml\");");

                provider.AddFile(Provider.DAO005, "SqlMaps/DAO005.xml", "Reports & Analytics");
                Console.WriteLine($"        provider.AddFile(Provider.{nameof(Provider.DAO005)}, \"SqlMaps/DAO005.xml\");");
            });
        });

        Console.WriteLine("    });");
        Console.WriteLine("});");
        Console.WriteLine();

        // Register services
        Console.WriteLine("// Đăng ký Services");
        Console.WriteLine("services.AddScoped<SimpleUserService>();      // Dùng 1 DAO");
        Console.WriteLine("services.AddScoped<ComplexBusinessService>(); // Dùng NHIỀU DAOs");
        services.AddScoped<SimpleUserService>();
        services.AddScoped<ComplexBusinessService>();

        Console.WriteLine();

        var serviceProvider = services.BuildServiceProvider();

        // ═══════════════════════════════════════════════════════════════
        // BƯỚC 3: Demo Single DAO Service
        // ═══════════════════════════════════════════════════════════════

        Console.WriteLine("BƯỚC 3: Single DAO Service - SimpleUserService");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine();

        Console.WriteLine("📁 File: Services/SimpleUserService.cs");
        Console.WriteLine("--------------------------------------------");
        Console.WriteLine("public class SimpleUserService : ProviderBasedRepository<User>");
        Console.WriteLine("{");
        Console.WriteLine("    private const string DAO_NAME = Provider.DAO001; // User");
        Console.WriteLine();
        Console.WriteLine("    public SimpleUserService(");
        Console.WriteLine("        IDbSessionFactory sessionFactory,");
        Console.WriteLine("        SqlMapProvider provider,");
        Console.WriteLine("        ILogger<SimpleUserService> logger)");
        Console.WriteLine("        : base(sessionFactory, provider, DAO_NAME, logger: logger)");
        Console.WriteLine("    {");
        Console.WriteLine("        // Provider resolve: Provider.DAO001 → \"SqlMaps/DAO001.xml\"");
        Console.WriteLine("    }");
        Console.WriteLine();
        Console.WriteLine("    // Methods chỉ sử dụng statements từ DAO001.xml");
        Console.WriteLine("    public async Task<User?> GetUserByIdAsync(int id)");
        Console.WriteLine("    {");
        Console.WriteLine("        return await QuerySingleAsync(\"User.GetUserById\", new { Id = id });");
        Console.WriteLine("    }");
        Console.WriteLine("}");
        Console.WriteLine();

        using (var scope = serviceProvider.CreateScope())
        {
            try
            {
                var userService = scope.ServiceProvider.GetRequiredService<SimpleUserService>();
                Console.WriteLine("✅ SimpleUserService initialized successfully");
                Console.WriteLine("   - Loaded DAO: DAO001 (User Management)");
                Console.WriteLine("   - Available methods: GetAllUsersAsync, GetUserByIdAsync, CreateUserAsync, etc.");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // BƯỚC 4: Demo Multiple DAOs Service
        // ═══════════════════════════════════════════════════════════════

        Console.WriteLine("BƯỚC 4: Multiple DAOs Service - ComplexBusinessService");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine();

        Console.WriteLine("📁 File: Services/ComplexBusinessService.cs");
        Console.WriteLine("--------------------------------------------");
        Console.WriteLine("public class ComplexBusinessService : MultiDaoProviderRepository<dynamic>");
        Console.WriteLine("{");
        Console.WriteLine("    private static readonly string[] DAO_NAMES = new[]");
        Console.WriteLine("    {");
        Console.WriteLine("        Provider.DAO001,  // User Management");
        Console.WriteLine("        Provider.DAO002,  // Product Management");
        Console.WriteLine("        Provider.DAO003   // Order Management");
        Console.WriteLine("    };");
        Console.WriteLine();
        Console.WriteLine("    public ComplexBusinessService(");
        Console.WriteLine("        IDbSessionFactory sessionFactory,");
        Console.WriteLine("        SqlMapProvider provider,");
        Console.WriteLine("        ILogger<ComplexBusinessService> logger)");
        Console.WriteLine("        : base(sessionFactory, provider, DAO_NAMES, logger: logger)");
        Console.WriteLine("    {");
        Console.WriteLine("        // Provider tự động resolve:");
        Console.WriteLine("        // Provider.DAO001 → \"SqlMaps/DAO001.xml\"");
        Console.WriteLine("        // Provider.DAO002 → \"SqlMaps/DAO002.xml\"");
        Console.WriteLine("        // Provider.DAO003 → \"SqlMaps/DAO003.xml\"");
        Console.WriteLine("    }");
        Console.WriteLine();
        Console.WriteLine("    // Methods sử dụng statements từ NHIỀU DAO files");
        Console.WriteLine("    public async Task<User?> GetUserByIdAsync(int id)");
        Console.WriteLine("    {");
        Console.WriteLine("        // Statement từ DAO001.xml");
        Console.WriteLine("        return await QuerySingleAsync<User>(\"User.GetUserById\", new { Id = id });");
        Console.WriteLine("    }");
        Console.WriteLine();
        Console.WriteLine("    public async Task<Product?> GetProductByIdAsync(int id)");
        Console.WriteLine("    {");
        Console.WriteLine("        // Statement từ DAO002.xml");
        Console.WriteLine("        return await QuerySingleAsync<Product>(\"Product.GetProductById\", new { Id = id });");
        Console.WriteLine("    }");
        Console.WriteLine();
        Console.WriteLine("    public async Task<IEnumerable<Order>> GetOrdersByUserAsync(int userId)");
        Console.WriteLine("    {");
        Console.WriteLine("        // Statement từ DAO003.xml");
        Console.WriteLine("        return await QueryListAsync<Order>(\"Order.GetOrdersByUser\", new { UserId = userId });");
        Console.WriteLine("    }");
        Console.WriteLine("}");
        Console.WriteLine();

        using (var scope = serviceProvider.CreateScope())
        {
            try
            {
                var complexService = scope.ServiceProvider.GetRequiredService<ComplexBusinessService>();
                Console.WriteLine("✅ ComplexBusinessService initialized successfully");
                Console.WriteLine("   - Loaded DAOs:");
                Console.WriteLine("     • DAO001 (User Management)");
                Console.WriteLine("     • DAO002 (Product Management)");
                Console.WriteLine("     • DAO003 (Order Management)");
                Console.WriteLine("   - Cross-domain methods:");
                Console.WriteLine("     • CreateOrderWithStockUpdateAsync() - Transaction across DAOs");
                Console.WriteLine("     • GetUserOrderSummaryAsync() - Query from 3 DAOs");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // BƯỚC 5: Validation & Summary
        // ═══════════════════════════════════════════════════════════════

        Console.WriteLine("BƯỚC 5: Provider Validation");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine();

        using (var scope = serviceProvider.CreateScope())
        {
            var provider = scope.ServiceProvider.GetRequiredService<SqlMapProvider>();

            Console.WriteLine("✅ Provider Pattern Verification");
            Console.WriteLine("--------------------------------------------");
            Console.WriteLine($"Total Registered Maps: {provider.Files.Count}");
            Console.WriteLine();

            foreach (var file in provider.Files)
            {
                Console.WriteLine($"  ✅ {file.Key} → {file.FilePath}");
                if (!string.IsNullOrEmpty(file.Description))
                    Console.WriteLine($"     Description: {file.Description}");
            }
            Console.WriteLine();

            // Validate all DAOs are registered
            Console.WriteLine("✅ Validation: All DaoNames registered?");
            Console.WriteLine("--------------------------------------------");
            var missingDaos = Provider.GetAllDaoNames()
                .Where(dao => !provider.HasFile(dao))
                .ToArray();

            if (missingDaos.Any())
            {
                Console.WriteLine($"❌ Missing DAOs: {string.Join(", ", missingDaos)}");
            }
            else
            {
                Console.WriteLine("✅ All DaoNames from Provider are registered!");
            }
            Console.WriteLine();
        }

        // ═══════════════════════════════════════════════════════════════
        // SUMMARY
        // ═══════════════════════════════════════════════════════════════

        Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  ✅ DEMO COMPLETED SUCCESSFULLY!                                 ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        Console.WriteLine("📊 Pattern Summary:");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine();

        Console.WriteLine("1️⃣  PROJECT DEFINES DaoNames (Models/Provider.cs)");
        Console.WriteLine("   ✅ IntelliSense: Provider.DAO001, Provider.DAO002, ...");
        Console.WriteLine("   ✅ Type-safe: Compiler check, no magic strings");
        Console.WriteLine("   ✅ Centralized: All DAO names in one place");
        Console.WriteLine();

        Console.WriteLine("2️⃣  PROGRAM.CS MAPS DaoNames → Files");
        Console.WriteLine("   ✅ Centralized configuration");
        Console.WriteLine("   ✅ Easy to change file locations");
        Console.WriteLine("   ✅ Support multiple connections");
        Console.WriteLine();

        Console.WriteLine("3️⃣  SERVICES USE DaoNames");
        Console.WriteLine("   ✅ Single DAO: ProviderBasedRepository<T>");
        Console.WriteLine("   ✅ Multiple DAOs: MultiDaoProviderRepository<T>");
        Console.WriteLine("   ✅ Cross-domain transactions");
        Console.WriteLine();

        Console.WriteLine("💡 Use Cases:");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("✅ Single DAO Service (SimpleUserService):");
        Console.WriteLine("   - Service chỉ cần 1 domain (User)");
        Console.WriteLine("   - Base class: ProviderBasedRepository<User>");
        Console.WriteLine("   - Example: UserService, ProductService, CategoryService");
        Console.WriteLine();
        Console.WriteLine("✅ Multiple DAOs Service (ComplexBusinessService):");
        Console.WriteLine("   - Service cần nhiều domains (User + Product + Order)");
        Console.WriteLine("   - Base class: MultiDaoProviderRepository<dynamic>");
        Console.WriteLine("   - Example: CheckSessionService, ReportService, DashboardService");
        Console.WriteLine();

        Console.WriteLine("📁 Project Structure:");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("MrFu.SmartCheck.Web/");
        Console.WriteLine("├── Models/");
        Console.WriteLine("│   └── Provider.cs              ← DaoNames definition");
        Console.WriteLine("├── Services/");
        Console.WriteLine("│   ├── AssetService.cs          ← Single DAO (DAO000)");
        Console.WriteLine("│   ├── CategoryService.cs       ← Single DAO (DAO001)");
        Console.WriteLine("│   ├── CheckSessionService.cs   ← Multiple DAOs (003, 004, 000)");
        Console.WriteLine("│   └── ReportService.cs         ← Multiple DAOs (009-013)");
        Console.WriteLine("├── DAO/");
        Console.WriteLine("│   ├── DAO000.xml               ← Assets SQL");
        Console.WriteLine("│   ├── DAO001.xml               ← Categories SQL");
        Console.WriteLine("│   └── ...");
        Console.WriteLine("└── Program.cs                   ← ConfigureSqlMaps()");
        Console.WriteLine();

        Console.WriteLine("🎯 Benefits:");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine("✅ Flexibility: Project tự định nghĩa DaoNames theo domain");
        Console.WriteLine("✅ Maintainability: Centralized configuration");
        Console.WriteLine("✅ Type Safety: IntelliSense + Compiler check");
        Console.WriteLine("✅ Scalability: Dễ dàng thêm DAO mới");
        Console.WriteLine("✅ Testability: Easy to mock SqlMapProvider");
        Console.WriteLine();

        Console.WriteLine("📚 Documentation:");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine("  - DAONAMES_MAPPING_GUIDE.md  - Hướng dẫn chi tiết ánh xạ DaoNames");
        Console.WriteLine("  - PROVIDER_PATTERN_GUIDE.md  - Provider pattern guide");
        Console.WriteLine("  - MULTI_CONNECTION_GUIDE.md  - Multiple connections guide");
        Console.WriteLine();

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
