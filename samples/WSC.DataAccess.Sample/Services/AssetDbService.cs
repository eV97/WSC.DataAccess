using Microsoft.Extensions.Logging;
using WSC.DataAccess.Core;
using WSC.DataAccess.Extensions;
using WSC.DataAccess.Sample.Models;

namespace WSC.DataAccess.Sample.Services;

/// <summary>
/// Demo service using ISql pattern (iBatis.NET style)
/// Pattern: Direct connection access with statement execution
/// </summary>
public class AssetDbService
{
    private readonly ISql _sql;
    private readonly ILogger<AssetDbService> _logger;

    public AssetDbService(ISql sql, ILogger<AssetDbService> logger)
    {
        _sql = sql;
        _logger = logger;
    }

    #region Assets - Main Database

    /// <summary>
    /// Get all assets from Main Database
    /// </summary>
    public async Task<List<Asset>> GetAllAssetsAsync()
    {
        try
        {
            // Set DAO context
            _sql.GetDAO(Provider.DAO000);

            // Create connection and execute
            using var connection = _sql.CreateConnection();
            var assets = await connection.StatementExecuteQueryAsync<Asset>("Asset.GetAll");

            _logger.LogInformation("Retrieved {Count} assets from MainDB", assets.Count());
            return assets.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all assets");
            throw;
        }
    }

    /// <summary>
    /// Get asset by ID
    /// </summary>
    public async Task<Asset?> GetAssetByIdAsync(int id)
    {
        try
        {
            _sql.GetDAO(Provider.DAO000);

            using var connection = _sql.CreateConnection();
            var asset = await connection.StatementExecuteSingleAsync<Asset>(
                "Asset.GetById",
                new { Id = id });

            _logger.LogInformation("Retrieved asset {AssetId}: {AssetName}",
                id, asset?.Name ?? "Not found");

            return asset;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving asset {AssetId}", id);
            throw;
        }
    }

    /// <summary>
    /// Count assets with filters
    /// </summary>
    public async Task<int> CountAssetsAsync(
        string? searchKeyword = null,
        int? categoryId = null)
    {
        try
        {
            _sql.GetDAO(Provider.DAO000);

            using var connection = _sql.CreateConnection();
            var count = await connection.StatementExecuteScalarAsync<int>(
                "Asset.Count",
                new
                {
                    SearchKeyword = searchKeyword,
                    CategoryId = categoryId
                });

            _logger.LogInformation("Counted {Count} assets with filters", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting assets");
            throw;
        }
    }

    /// <summary>
    /// Get assets with pagination
    /// </summary>
    public async Task<List<Asset>> GetAssetsPaginatedAsync(
        int page,
        int pageSize,
        string? searchKeyword = null,
        int? categoryId = null)
    {
        try
        {
            _sql.GetDAO(Provider.DAO000);

            var offset = (page - 1) * pageSize;

            using var connection = _sql.CreateConnection();
            var assets = await connection.StatementExecuteQueryAsync<Asset>(
                "Asset.GetPaginated",
                new
                {
                    Offset = offset,
                    PageSize = pageSize,
                    SearchKeyword = searchKeyword,
                    CategoryId = categoryId
                });

            _logger.LogInformation("Retrieved {Count} assets (page {Page}, size {PageSize})",
                assets.Count(), page, pageSize);

            return assets.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated assets");
            throw;
        }
    }

    /// <summary>
    /// Create new asset
    /// </summary>
    public async Task<int> CreateAssetAsync(Asset asset)
    {
        try
        {
            _sql.GetDAO(Provider.DAO000);

            using var connection = _sql.CreateConnection();
            var rowsAffected = await connection.StatementExecuteAsync(
                "Asset.Insert",
                asset);

            _logger.LogInformation("Created asset: {AssetName}", asset.Name);
            return rowsAffected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating asset");
            throw;
        }
    }

    /// <summary>
    /// Update existing asset
    /// </summary>
    public async Task<int> UpdateAssetAsync(Asset asset)
    {
        try
        {
            _sql.GetDAO(Provider.DAO000);

            using var connection = _sql.CreateConnection();
            var rowsAffected = await connection.StatementExecuteAsync(
                "Asset.Update",
                asset);

            _logger.LogInformation("Updated asset {AssetId}: {AssetName}",
                asset.Id, asset.Name);

            return rowsAffected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating asset {AssetId}", asset.Id);
            throw;
        }
    }

    #endregion

    #region Cross-DAO Operations

    /// <summary>
    /// Get asset with category details (cross-DAO)
    /// Demonstrates switching between DAOs
    /// </summary>
    public async Task<AssetWithCategory> GetAssetWithCategoryAsync(int assetId)
    {
        try
        {
            // Get asset from DAO000
            _sql.GetDAO(Provider.DAO000);
            using var conn1 = _sql.CreateConnection();
            var asset = await conn1.StatementExecuteSingleAsync<Asset>(
                "Asset.GetById",
                new { Id = assetId });

            if (asset == null)
            {
                throw new InvalidOperationException($"Asset {assetId} not found");
            }

            // Get category from DAO001 (if asset has category)
            Category? category = null;
            if (asset.CategoryId > 0)
            {
                _sql.GetDAO(Provider.DAO001);
                using var conn2 = _sql.CreateConnection();
                category = await conn2.StatementExecuteSingleAsync<Category>(
                    "Category.GetById",
                    new { Id = asset.CategoryId });
            }

            _logger.LogInformation("Retrieved asset {AssetId} with category {CategoryName}",
                assetId, category?.Name ?? "None");

            return new AssetWithCategory
            {
                Asset = asset,
                Category = category
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving asset with category");
            throw;
        }
    }

    #endregion

    #region Multiple Connections (MainDB vs ReportDB)

    /// <summary>
    /// Get assets from Report Database
    /// Report DB might be a read replica or ETL database
    /// </summary>
    public async Task<List<Asset>> GetAssetsForReportAsync()
    {
        try
        {
            // Set DAO for ReportDB connection
            _sql.GetDAO(Provider.DAO000, "ReportDB");

            // Create connection to ReportDB
            using var connection = _sql.CreateConnection("ReportDB");
            var assets = await connection.StatementExecuteQueryAsync<Asset>("Asset.GetAll");

            _logger.LogInformation("Retrieved {Count} assets from ReportDB", assets.Count());
            return assets.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assets from ReportDB");
            throw;
        }
    }

    /// <summary>
    /// Get asset summary from Report Database
    /// </summary>
    public async Task<AssetSummary?> GetAssetSummaryAsync()
    {
        try
        {
            _sql.GetDAO(Provider.DAO000, "ReportDB");

            using var connection = _sql.CreateConnection("ReportDB");
            var summary = await connection.StatementExecuteSingleAsync<AssetSummary>(
                "Asset.GetSummary");

            _logger.LogInformation("Retrieved asset summary from ReportDB: {TotalAssets} total",
                summary?.TotalAssets ?? 0);

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving asset summary from ReportDB");
            throw;
        }
    }

    #endregion

    #region Transaction Support

    /// <summary>
    /// Create asset with transaction
    /// Demonstrates transaction across multiple statements
    /// </summary>
    public async Task<int> CreateAssetWithHistoryAsync(Asset asset, string createdBy)
    {
        try
        {
            _sql.GetDAO(Provider.DAO000);

            using var connection = _sql.CreateConnection();

            return await connection.ExecuteInTransactionAsync(async conn =>
            {
                // 1. Insert asset
                var assetId = await conn.StatementExecuteAsync("Asset.Insert", asset);

                // 2. Insert history record
                await conn.StatementExecuteAsync(
                    "Asset.InsertHistory",
                    new
                    {
                        AssetId = assetId,
                        Action = "Created",
                        ActionBy = createdBy,
                        ActionDate = DateTime.Now
                    });

                _logger.LogInformation("Created asset {AssetId} with history by {User}",
                    assetId, createdBy);

                return assetId;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating asset with history");
            throw;
        }
    }

    #endregion
}

#region Supporting Models

public class AssetWithCategory
{
    public Asset Asset { get; set; } = null!;
    public Category? Category { get; set; }
}

public class AssetSummary
{
    public int TotalAssets { get; set; }
    public int ActiveAssets { get; set; }
    public int InactiveAssets { get; set; }
    public decimal TotalValue { get; set; }
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

#endregion
