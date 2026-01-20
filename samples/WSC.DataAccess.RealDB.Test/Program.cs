using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Core;
using Dapper;

Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║  WSC.DataAccess - Real Database Connection Test                 ║");
Console.WriteLine("║  Database: LP_ApplicationSystem                                  ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
Console.WriteLine();

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

// Get connection string
var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found");

Console.WriteLine("📋 Connection Info:");
Console.WriteLine($"   Server: FHC-VUONGLH3\\SQLEXPRESS02");
Console.WriteLine($"   Database: LP_ApplicationSystem");
Console.WriteLine($"   User: admin");
Console.WriteLine();

// Setup DI
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddWscDataAccess(connectionString);
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var sessionFactory = services.GetRequiredService<IDbSessionFactory>();

    try
    {
        // Test 1: Connection Test
        Console.WriteLine("🔌 TEST 1: Testing Connection...");
        Console.WriteLine("   Connecting to database...");

        using (var session = sessionFactory.OpenSession())
        {
            var result = await session.Connection.ExecuteScalarAsync<int>("SELECT 1");
            if (result == 1)
            {
                Console.WriteLine("   ✅ Connection successful!");
            }
        }
        Console.WriteLine();

        // Test 2: Get Database Info
        Console.WriteLine("📊 TEST 2: Getting Database Information...");

        using (var session = sessionFactory.OpenSession())
        {
            // Get database name
            var dbName = await session.Connection.ExecuteScalarAsync<string>(
                "SELECT DB_NAME()");
            Console.WriteLine($"   Current Database: {dbName}");

            // Get SQL Server version
            var version = await session.Connection.ExecuteScalarAsync<string>(
                "SELECT @@VERSION");
            var versionShort = version?.Split('\n')[0] ?? "Unknown";
            Console.WriteLine($"   SQL Server Version: {versionShort}");
        }
        Console.WriteLine();

        // Test 3: List All Tables
        Console.WriteLine("📁 TEST 3: Listing All Tables in Database...");

        using (var session = sessionFactory.OpenSession())
        {
            var tablesSql = @"
                SELECT
                    TABLE_SCHEMA,
                    TABLE_NAME,
                    TABLE_TYPE
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'
                ORDER BY TABLE_NAME";

            var tables = await session.Connection.QueryAsync<TableInfo>(tablesSql);
            var tablesList = tables.ToList();

            Console.WriteLine($"   Found {tablesList.Count} tables:");
            Console.WriteLine();

            int count = 0;
            foreach (var table in tablesList)
            {
                count++;
                Console.WriteLine($"   {count,3}. [{table.TABLE_SCHEMA}].[{table.TABLE_NAME}]");

                // Show first 5 columns of each table
                if (count <= 5) // Only show details for first 5 tables
                {
                    var columnsSql = @"
                        SELECT
                            COLUMN_NAME,
                            DATA_TYPE,
                            IS_NULLABLE
                        FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_SCHEMA = @Schema
                          AND TABLE_NAME = @TableName
                        ORDER BY ORDINAL_POSITION";

                    var columns = await session.Connection.QueryAsync<ColumnInfo>(
                        columnsSql,
                        new { Schema = table.TABLE_SCHEMA, TableName = table.TABLE_NAME });

                    var columnsList = columns.Take(5).ToList();
                    foreach (var col in columnsList)
                    {
                        Console.WriteLine($"        └─ {col.COLUMN_NAME} ({col.DATA_TYPE}) {(col.IS_NULLABLE == "YES" ? "NULL" : "NOT NULL")}");
                    }

                    if (columns.Count() > 5)
                    {
                        Console.WriteLine($"        └─ ... và {columns.Count() - 5} cột khác");
                    }
                    Console.WriteLine();
                }
            }
        }
        Console.WriteLine();

        // Test 4: Query First Table Data
        Console.WriteLine("📝 TEST 4: Querying Sample Data from First Table...");

        using (var session = sessionFactory.OpenSession())
        {
            // Get first table
            var firstTableSql = @"
                SELECT TOP 1 TABLE_SCHEMA, TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'
                ORDER BY TABLE_NAME";

            var firstTable = await session.Connection.QueryFirstOrDefaultAsync<TableInfo>(firstTableSql);

            if (firstTable != null)
            {
                Console.WriteLine($"   Table: [{firstTable.TABLE_SCHEMA}].[{firstTable.TABLE_NAME}]");

                try
                {
                    // Get row count
                    var countSql = $"SELECT COUNT(*) FROM [{firstTable.TABLE_SCHEMA}].[{firstTable.TABLE_NAME}]";
                    var rowCount = await session.Connection.ExecuteScalarAsync<int>(countSql);
                    Console.WriteLine($"   Total Rows: {rowCount:N0}");

                    if (rowCount > 0)
                    {
                        // Get sample data (top 3 rows)
                        var dataSql = $"SELECT TOP 3 * FROM [{firstTable.TABLE_SCHEMA}].[{firstTable.TABLE_NAME}]";
                        var data = await session.Connection.QueryAsync(dataSql);
                        var dataList = data.ToList();

                        Console.WriteLine($"   Sample Data (Top 3 rows):");
                        Console.WriteLine();

                        int rowNum = 0;
                        foreach (var row in dataList)
                        {
                            rowNum++;
                            Console.WriteLine($"   Row {rowNum}:");

                            var dict = (IDictionary<string, object>)row;
                            foreach (var kvp in dict.Take(5)) // Show first 5 columns
                            {
                                var value = kvp.Value?.ToString() ?? "NULL";
                                if (value.Length > 50)
                                    value = value.Substring(0, 47) + "...";

                                Console.WriteLine($"      {kvp.Key}: {value}");
                            }

                            if (dict.Count > 5)
                            {
                                Console.WriteLine($"      ... và {dict.Count - 5} cột khác");
                            }
                            Console.WriteLine();
                        }
                    }
                    else
                    {
                        Console.WriteLine("   ⚠️  Table is empty (no data)");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠️  Could not query data: {ex.Message}");
                }
            }
        }
        Console.WriteLine();

        // Test 5: Search for specific tables (common names)
        Console.WriteLine("🔍 TEST 5: Searching for Common Table Patterns...");

        using (var session = sessionFactory.OpenSession())
        {
            var patterns = new[] { "User", "Product", "Order", "Customer", "Employee", "Application", "System" };

            foreach (var pattern in patterns)
            {
                var searchSql = @"
                    SELECT TABLE_SCHEMA, TABLE_NAME
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_TYPE = 'BASE TABLE'
                      AND TABLE_NAME LIKE @Pattern
                    ORDER BY TABLE_NAME";

                var results = await session.Connection.QueryAsync<TableInfo>(
                    searchSql,
                    new { Pattern = $"%{pattern}%" });

                var resultsList = results.ToList();
                if (resultsList.Any())
                {
                    Console.WriteLine($"   '{pattern}' tables found ({resultsList.Count}):");
                    foreach (var table in resultsList.Take(5))
                    {
                        Console.WriteLine($"      - [{table.TABLE_SCHEMA}].[{table.TABLE_NAME}]");
                    }
                    if (resultsList.Count > 5)
                    {
                        Console.WriteLine($"      ... và {resultsList.Count - 5} bảng khác");
                    }
                }
            }
        }
        Console.WriteLine();

        // Test 6: Execute Custom Query
        Console.WriteLine("⚡ TEST 6: Custom Query Examples...");
        Console.WriteLine("   You can now write custom queries using this connection!");
        Console.WriteLine();
        Console.WriteLine("   Example code:");
        Console.WriteLine(@"
   using (var session = sessionFactory.OpenSession())
   {
       var sql = ""SELECT TOP 10 * FROM YourTable"";
       var data = await session.Connection.QueryAsync<YourModel>(sql);

       foreach (var item in data)
       {
           Console.WriteLine(item);
       }
   }
");

        Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  ✅ ALL TESTS PASSED SUCCESSFULLY!                               ║");
        Console.WriteLine("║  Connection to LP_ApplicationSystem database is working!        ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
    }
    catch (Exception ex)
    {
        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  ❌ ERROR OCCURRED                                                ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine($"Error Type: {ex.GetType().Name}");
        Console.WriteLine($"Message: {ex.Message}");
        Console.WriteLine();
        Console.WriteLine("Stack Trace:");
        Console.WriteLine(ex.StackTrace);
        Console.WriteLine();
        Console.WriteLine("Possible Solutions:");
        Console.WriteLine("1. Check if SQL Server is running");
        Console.WriteLine("2. Verify server name: FHC-VUONGLH3\\SQLEXPRESS02");
        Console.WriteLine("3. Verify credentials: admin/admin");
        Console.WriteLine("4. Check if LP_ApplicationSystem database exists");
        Console.WriteLine("5. Ensure SQL Server authentication is enabled");
        Console.WriteLine("6. Check firewall settings");
    }
}

Console.WriteLine();
Console.WriteLine("Press any key to exit...");
Console.ReadKey();

// Helper classes for mapping
public class TableInfo
{
    public string TABLE_SCHEMA { get; set; } = string.Empty;
    public string TABLE_NAME { get; set; } = string.Empty;
    public string? TABLE_TYPE { get; set; }
}

public class ColumnInfo
{
    public string COLUMN_NAME { get; set; } = string.Empty;
    public string DATA_TYPE { get; set; } = string.Empty;
    public string IS_NULLABLE { get; set; } = string.Empty;
}
