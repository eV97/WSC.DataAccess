using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Constants;
using WSC.DataAccess.Core;
using WSC.DataAccess.Repository;
using WSC.DataAccess.Sample.Models;

namespace WSC.DataAccess.Sample.Repositories;

/// <summary>
/// User Repository - DAO001
/// User management operations
/// </summary>
public class UserRepository : ProviderBasedRepository<User>
{
    private const string DAO_NAME = DaoNames.DAO001;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider,
        ILogger<UserRepository> logger)
        : base(sessionFactory, provider, DAO_NAME)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        _logger.LogInformation("Getting all users");
        return await QueryListAsync("User.GetAllUsers");
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        _logger.LogInformation("Getting user by ID: {UserId}", id);
        return await QuerySingleAsync("User.GetUserById", new { Id = id });
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        _logger.LogInformation("Getting user by username: {Username}", username);
        return await QuerySingleAsync("User.GetUserByUsername", new { Username = username });
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        _logger.LogInformation("Getting user by email: {Email}", email);
        return await QuerySingleAsync("User.GetUserByEmail", new { Email = email });
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        _logger.LogInformation("Getting active users");
        return await QueryListAsync("User.GetActiveUsers");
    }

    public async Task<int> GetUserCountAsync()
    {
        var result = await QuerySingleAsync("User.GetUserCount");
        return Convert.ToInt32(result ?? 0);
    }

    public async Task<int> InsertUserAsync(User user)
    {
        _logger.LogInformation("Inserting user: {Username}", user.Username);
        return await ExecuteAsync("User.InsertUser", user);
    }

    public async Task<int> UpdateUserAsync(User user)
    {
        _logger.LogInformation("Updating user: {UserId}", user.Id);
        return await ExecuteAsync("User.UpdateUser", user);
    }

    public async Task<int> DeleteUserAsync(int id)
    {
        _logger.LogInformation("Deleting user: {UserId}", id);
        return await ExecuteAsync("User.DeleteUser", new { Id = id });
    }

    public async Task<int> DeactivateUserAsync(int id)
    {
        _logger.LogInformation("Deactivating user: {UserId}", id);
        return await ExecuteAsync("User.DeactivateUser", new { Id = id });
    }

    public async Task<int> ActivateUserAsync(int id)
    {
        _logger.LogInformation("Activating user: {UserId}", id);
        return await ExecuteAsync("User.ActivateUser", new { Id = id });
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        var result = await QuerySingleAsync("User.UsernameExists", new { Username = username });
        return Convert.ToBoolean(result ?? false);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        var result = await QuerySingleAsync("User.EmailExists", new { Email = email });
        return Convert.ToBoolean(result ?? false);
    }
}
