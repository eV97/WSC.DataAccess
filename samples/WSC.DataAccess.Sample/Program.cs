using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Examples.Models;
using WSC.DataAccess.Examples.Repositories;

namespace WSC.DataAccess.Sample;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== WSC Data Access Sample Application ===\n");

        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        // Setup dependency injection
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging((context, logging) =>
            {
                // Configure iBatis logging
                logging.ClearProviders();
                logging.AddIBatisLogging();

                Console.WriteLine("✓ iBatis logging configured");
                Console.WriteLine($"   Log directory: log/iBatis/");
                Console.WriteLine($"   Log files: ibatis-YYYYMMDD.log, ibatis-errors-YYYYMMDD.log\n");
            })
            .ConfigureServices((context, services) =>
            {
                // Get connection string
                var connectionString = configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string not found");

                // Register WSC Data Access services
                services.AddWscDataAccess(connectionString, options =>
                {
                    // Add named connections
                    var reportingConnection = configuration.GetConnectionString("ReportingConnection");
                    if (!string.IsNullOrEmpty(reportingConnection))
                    {
                        options.AddConnection("Reporting", reportingConnection);
                    }

                    // Add SQL map files (for IBatis-style repositories)
                    var sqlMapPath = Path.Combine(AppContext.BaseDirectory, "SqlMaps", "ProductMap.xml");
                    if (File.Exists(sqlMapPath))
                    {
                        options.AddSqlMapFile(sqlMapPath);
                    }
                });

                // Register repositories
                services.AddScoped<UserRepository>();
                services.AddScoped<ProductRepository>();
            })
            .Build();

        using (var scope = host.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            try
            {
                await RunUserRepositoryExampleAsync(services);
                Console.WriteLine("\n" + new string('-', 60) + "\n");
                await RunProductRepositoryExampleAsync(services);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static async Task RunUserRepositoryExampleAsync(IServiceProvider services)
    {
        Console.WriteLine("=== User Repository Example (BaseRepository Pattern) ===\n");

        var userRepo = services.GetRequiredService<UserRepository>();

        // Example 1: Get all users
        Console.WriteLine("1. Getting all users...");
        try
        {
            var users = await userRepo.GetAllAsync();
            Console.WriteLine($"   Found {users.Count()} users");
            foreach (var user in users.Take(5))
            {
                Console.WriteLine($"   - {user.Username} ({user.Email})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Error: {ex.Message}");
        }

        // Example 2: Insert a new user
        Console.WriteLine("\n2. Inserting a new user...");
        try
        {
            var newUser = new User
            {
                Username = $"user_{DateTime.Now.Ticks}",
                Email = $"user_{DateTime.Now.Ticks}@example.com",
                FullName = "Sample User",
                CreatedDate = DateTime.Now,
                IsActive = true
            };

            var newUserId = await userRepo.InsertAsync(newUser);
            Console.WriteLine($"   New user created with ID: {newUserId}");

            // Get the created user
            var createdUser = await userRepo.GetByIdAsync(newUserId);
            if (createdUser != null)
            {
                Console.WriteLine($"   Retrieved: {createdUser.Username} - {createdUser.FullName}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Error: {ex.Message}");
        }

        // Example 3: Get active users
        Console.WriteLine("\n3. Getting active users...");
        try
        {
            var activeUsers = await userRepo.GetActiveUsersAsync();
            Console.WriteLine($"   Found {activeUsers.Count()} active users");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Error: {ex.Message}");
        }

        // Example 4: Get user by username
        Console.WriteLine("\n4. Getting user by username...");
        try
        {
            var user = await userRepo.GetByUsernameAsync("admin");
            if (user != null)
            {
                Console.WriteLine($"   Found: {user.FullName} ({user.Email})");
            }
            else
            {
                Console.WriteLine("   User not found");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Error: {ex.Message}");
        }
    }

    static async Task RunProductRepositoryExampleAsync(IServiceProvider services)
    {
        Console.WriteLine("=== Product Repository Example (SqlMapRepository Pattern - IBatis Style) ===\n");

        var productRepo = services.GetRequiredService<ProductRepository>();

        // Example 1: Get all products
        Console.WriteLine("1. Getting all products (using SqlMap)...");
        try
        {
            var products = await productRepo.GetAllProductsAsync();
            Console.WriteLine($"   Found {products.Count()} products");
            foreach (var product in products.Take(5))
            {
                Console.WriteLine($"   - {product.ProductCode}: {product.ProductName} - ${product.Price}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Error: {ex.Message}");
        }

        // Example 2: Get product by ID
        Console.WriteLine("\n2. Getting product by ID (using SqlMap)...");
        try
        {
            var product = await productRepo.GetByIdAsync(1);
            if (product != null)
            {
                Console.WriteLine($"   Found: {product.ProductName}");
                Console.WriteLine($"   Price: ${product.Price}");
                Console.WriteLine($"   Stock: {product.StockQuantity}");
            }
            else
            {
                Console.WriteLine("   Product not found");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Error: {ex.Message}");
        }

        // Example 3: Insert a new product
        Console.WriteLine("\n3. Inserting a new product (using SqlMap)...");
        try
        {
            var newProduct = new Product
            {
                ProductCode = $"PRD{DateTime.Now.Ticks}",
                ProductName = "Sample Product",
                Description = "This is a sample product",
                Price = 99.99M,
                StockQuantity = 100,
                Category = "Electronics",
                CreatedDate = DateTime.Now,
                IsActive = true
            };

            var result = await productRepo.InsertAsync(newProduct);
            Console.WriteLine($"   Product inserted: {result} row(s) affected");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Error: {ex.Message}");
        }

        // Example 4: Get products by category
        Console.WriteLine("\n4. Getting products by category (using SqlMap)...");
        try
        {
            var products = await productRepo.GetByCategoryAsync("Electronics");
            Console.WriteLine($"   Found {products.Count()} products in Electronics category");
            foreach (var product in products.Take(3))
            {
                Console.WriteLine($"   - {product.ProductName}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Error: {ex.Message}");
        }

        // Example 5: Get low stock products
        Console.WriteLine("\n5. Getting low stock products (using SqlMap)...");
        try
        {
            var lowStockProducts = await productRepo.GetLowStockProductsAsync(10);
            Console.WriteLine($"   Found {lowStockProducts.Count()} products with stock <= 10");
            foreach (var product in lowStockProducts.Take(3))
            {
                Console.WriteLine($"   - {product.ProductName}: {product.StockQuantity} units");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Error: {ex.Message}");
        }
    }
}
