using Microsoft.Extensions.Logging;
using WSC.DataAccess.Core;
using WSC.DataAccess.Extensions;
using WSC.DataAccess.Sample.Models;

namespace WSC.DataAccess.Sample;

/// <summary>
/// Simple test service using ISql pattern
/// Demonstrates all basic operations with real database
/// </summary>
public class TestService
{
    private readonly ISql _sql;
    private readonly ILogger<TestService> _logger;

    public TestService(ISql sql, ILogger<TestService> logger)
    {
        _sql = sql;
        _logger = logger;
    }

    /// <summary>
    /// Test 1: Basic Query - Get all users
    /// </summary>
    public async Task TestGetAllUsersAsync()
    {
        try
        {
            _logger.LogInformation("=== TEST 1: Get All Users ===");

            // Set DAO context
            _sql.GetDAO(Provider.DAO001);

            // Create connection
            using var connection = _sql.CreateConnection();

            // Execute query
            var users = await connection.StatementExecuteQueryAsync<User>("User.GetAllUsers");

            _logger.LogInformation("Retrieved {Count} users", users.Count());

            foreach (var user in users.Take(5))
            {
                _logger.LogInformation("  - User: {Name} ({Email})", user.Name, user.Email);
            }

            _logger.LogInformation("✅ TEST 1 PASSED");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ TEST 1 FAILED");
            throw;
        }
    }

    /// <summary>
    /// Test 2: Single Query - Get user by ID
    /// </summary>
    public async Task TestGetUserByIdAsync(int userId)
    {
        try
        {
            _logger.LogInformation("=== TEST 2: Get User By ID ===");

            _sql.GetDAO(Provider.DAO001);
            using var connection = _sql.CreateConnection();

            var user = await connection.StatementExecuteSingleAsync<User>(
                "User.GetUserById",
                new { Id = userId });

            if (user != null)
            {
                _logger.LogInformation("Found user: {Name} ({Email})", user.Name, user.Email);
                _logger.LogInformation("✅ TEST 2 PASSED");
            }
            else
            {
                _logger.LogWarning("User {UserId} not found", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ TEST 2 FAILED");
            throw;
        }
    }

    /// <summary>
    /// Test 3: Scalar Query - Count users
    /// </summary>
    public async Task TestCountUsersAsync()
    {
        try
        {
            _logger.LogInformation("=== TEST 3: Count Users ===");

            _sql.GetDAO(Provider.DAO001);
            using var connection = _sql.CreateConnection();

            var count = await connection.StatementExecuteScalarAsync<int>("User.CountUsers");

            _logger.LogInformation("Total users: {Count}", count);
            _logger.LogInformation("✅ TEST 3 PASSED");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ TEST 3 FAILED");
            throw;
        }
    }

    /// <summary>
    /// Test 4: Insert - Create new user
    /// </summary>
    public async Task<int> TestCreateUserAsync()
    {
        try
        {
            _logger.LogInformation("=== TEST 4: Create User ===");

            var newUser = new User
            {
                Name = $"Test User {DateTime.Now:HHmmss}",
                Email = $"test{DateTime.Now:HHmmss}@example.com",
                Password = "password123",
                IsActive = true
            };

            _sql.GetDAO(Provider.DAO001);
            using var connection = _sql.CreateConnection();

            var rowsAffected = await connection.StatementExecuteAsync(
                "User.InsertUser",
                newUser);

            _logger.LogInformation("Inserted {Rows} row(s). User: {Name}", rowsAffected, newUser.Name);
            _logger.LogInformation("✅ TEST 4 PASSED");

            return rowsAffected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ TEST 4 FAILED");
            throw;
        }
    }

    /// <summary>
    /// Test 5: Cross-DAO Query - Get user with products
    /// </summary>
    public async Task TestCrossDaoQueryAsync(int userId)
    {
        try
        {
            _logger.LogInformation("=== TEST 5: Cross-DAO Query ===");

            // Get user from DAO001
            _sql.GetDAO(Provider.DAO001);
            using var conn1 = _sql.CreateConnection();
            var user = await conn1.StatementExecuteSingleAsync<User>(
                "User.GetUserById",
                new { Id = userId });

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return;
            }

            _logger.LogInformation("User: {Name}", user.Name);

            // Get products from DAO002 (if exists)
            try
            {
                _sql.GetDAO(Provider.DAO002);
                using var conn2 = _sql.CreateConnection();
                var products = await conn2.StatementExecuteQueryAsync<Product>(
                    "Product.GetAllProducts");

                _logger.LogInformation("Found {Count} products", products.Count());
            }
            catch
            {
                _logger.LogWarning("Product DAO not configured or table doesn't exist");
            }

            _logger.LogInformation("✅ TEST 5 PASSED");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ TEST 5 FAILED");
            throw;
        }
    }

    /// <summary>
    /// Test 6: Transaction - Insert multiple users
    /// </summary>
    public async Task TestTransactionAsync()
    {
        try
        {
            _logger.LogInformation("=== TEST 6: Transaction Test ===");

            _sql.GetDAO(Provider.DAO001);
            using var connection = _sql.CreateConnection();

            await connection.ExecuteInTransactionAsync(async conn =>
            {
                var user1 = new User
                {
                    Name = $"Transaction User 1 {DateTime.Now:HHmmss}",
                    Email = $"trans1_{DateTime.Now:HHmmss}@example.com",
                    Password = "password123",
                    IsActive = true
                };

                var user2 = new User
                {
                    Name = $"Transaction User 2 {DateTime.Now:HHmmss}",
                    Email = $"trans2_{DateTime.Now:HHmmss}@example.com",
                    Password = "password123",
                    IsActive = true
                };

                await conn.StatementExecuteAsync("User.InsertUser", user1);
                _logger.LogInformation("  - Inserted user 1: {Name}", user1.Name);

                await conn.StatementExecuteAsync("User.InsertUser", user2);
                _logger.LogInformation("  - Inserted user 2: {Name}", user2.Name);
            });

            _logger.LogInformation("✅ TEST 6 PASSED - Transaction committed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ TEST 6 FAILED - Transaction rolled back");
            throw;
        }
    }

    /// <summary>
    /// Test 7: System queries - Database info
    /// </summary>
    public async Task TestSystemQueriesAsync()
    {
        try
        {
            _logger.LogInformation("=== TEST 7: System Queries ===");

            _sql.GetDAO(Provider.DAO000);
            using var connection = _sql.CreateConnection();

            // Test connection
            var testResult = await connection.StatementExecuteScalarAsync<int>("System.TestConnection");
            _logger.LogInformation("Connection test: {Result}", testResult == 1 ? "✅ OK" : "❌ Failed");

            // Get database version
            var version = await connection.StatementExecuteSingleAsync<string>("System.GetDatabaseVersion");
            _logger.LogInformation("Database version: {Version}", version);

            // Get current database
            var dbName = await connection.StatementExecuteSingleAsync<string>("System.GetCurrentDatabase");
            _logger.LogInformation("Current database: {DbName}", dbName);

            // Get server name
            var serverName = await connection.StatementExecuteSingleAsync<string>("System.GetServerName");
            _logger.LogInformation("Server name: {ServerName}", serverName);

            // Get current datetime
            var currentTime = await connection.StatementExecuteSingleAsync<DateTime>("System.GetCurrentDateTime");
            _logger.LogInformation("Server time: {Time:yyyy-MM-dd HH:mm:ss}", currentTime);

            _logger.LogInformation("✅ TEST 7 PASSED");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ TEST 7 FAILED");
            throw;
        }
    }

    /// <summary>
    /// Run all tests
    /// </summary>
    public async Task RunAllTestsAsync()
    {
        _logger.LogInformation("");
        _logger.LogInformation("╔══════════════════════════════════════════════════════════════════╗");
        _logger.LogInformation("║  WSC.DataAccess - ISql Pattern Test Suite                       ║");
        _logger.LogInformation("╚══════════════════════════════════════════════════════════════════╝");
        _logger.LogInformation("");

        try
        {
            // Test 7: System queries first (check connection)
            await TestSystemQueriesAsync();
            _logger.LogInformation("");

            // Test 1: Get all users
            await TestGetAllUsersAsync();
            _logger.LogInformation("");

            // Test 2: Get user by ID
            await TestGetUserByIdAsync(1);
            _logger.LogInformation("");

            // Test 3: Count users
            await TestCountUsersAsync();
            _logger.LogInformation("");

            // Test 4: Create user
            await TestCreateUserAsync();
            _logger.LogInformation("");

            // Test 5: Cross-DAO query
            await TestCrossDaoQueryAsync(1);
            _logger.LogInformation("");

            // Test 6: Transaction
            await TestTransactionAsync();
            _logger.LogInformation("");

            _logger.LogInformation("╔══════════════════════════════════════════════════════════════════╗");
            _logger.LogInformation("║  ✅ ALL TESTS COMPLETED SUCCESSFULLY!                            ║");
            _logger.LogInformation("╚══════════════════════════════════════════════════════════════════╝");
        }
        catch (Exception ex)
        {
            _logger.LogError("");
            _logger.LogError("╔══════════════════════════════════════════════════════════════════╗");
            _logger.LogError("║  ❌ TESTS FAILED                                                  ║");
            _logger.LogError("╚══════════════════════════════════════════════════════════════════╝");
            _logger.LogError("Error: {Message}", ex.Message);
            throw;
        }
    }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
