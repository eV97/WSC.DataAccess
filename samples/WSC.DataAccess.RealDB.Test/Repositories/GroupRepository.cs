using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Constants;
using WSC.DataAccess.Core;
using WSC.DataAccess.Repository;

namespace WSC.DataAccess.RealDB.Test.Repositories;

/// <summary>
/// ✨ Example: Group Repository giống MrFu.SmartCheck pattern
/// Sử dụng DaoNames constants thay vì hardcode strings
/// </summary>
public class GroupRepository : ProviderBasedRepository<Group>
{
    // ✨ Sử dụng DAO constants thay vì hardcode "DAO003"
    private const string DAO_NAME = DaoNames.DAO003;

    private readonly ILogger<GroupRepository> _logger;

    public GroupRepository(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider,
        ILogger<GroupRepository> logger)
        : base(sessionFactory, provider, DAO_NAME, SqlMapProvider.DEFAULT_CONNECTION)
    {
        _logger = logger;
    }

    /// <summary>
    /// Lấy groups theo user ID - Giống MrFu.SmartCheck pattern
    /// </summary>
    public async Task<IEnumerable<Group>> GetGroupsByUserAsync(int userId)
    {
        try
        {
            var parameters = new { UserId = userId };

            // ✨ Gọi QueryListAsync với statement ID
            return await QueryListAsync("GetGroupsByUser", parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting groups by user: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Lấy tất cả groups
    /// </summary>
    public async Task<IEnumerable<Group>> GetAllGroupsAsync()
    {
        try
        {
            return await QueryListAsync("GetAllGroups");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all groups");
            throw;
        }
    }

    /// <summary>
    /// Lấy group theo ID
    /// </summary>
    public async Task<Group?> GetGroupByIdAsync(int groupId)
    {
        try
        {
            var parameters = new { GroupId = groupId };
            return await QuerySingleAsync("GetGroupById", parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting group by ID: {GroupId}", groupId);
            throw;
        }
    }

    /// <summary>
    /// Tạo group mới
    /// </summary>
    public async Task<int> CreateGroupAsync(Group group)
    {
        try
        {
            return await ExecuteAsync("InsertGroup", group);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group: {GroupName}", group.Name);
            throw;
        }
    }

    /// <summary>
    /// Update group
    /// </summary>
    public async Task<int> UpdateGroupAsync(Group group)
    {
        try
        {
            return await ExecuteAsync("UpdateGroup", group);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating group: {GroupId}", group.Id);
            throw;
        }
    }

    /// <summary>
    /// Delete group
    /// </summary>
    public async Task<int> DeleteGroupAsync(int groupId)
    {
        try
        {
            var parameters = new { GroupId = groupId };
            return await ExecuteAsync("DeleteGroup", parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting group: {GroupId}", groupId);
            throw;
        }
    }
}

/// <summary>
/// Group model
/// </summary>
public class Group
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
