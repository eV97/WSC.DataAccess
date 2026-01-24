using Microsoft.Extensions.Logging;
using WSC.DataAccess.Configuration;
using WSC.DataAccess.Core;
using WSC.DataAccess.Repository;
using WSC.DataAccess.Sample.Models;

namespace WSC.DataAccess.Sample.Services;

/// <summary>
/// Demo service sử dụng 1 DAO duy nhất (Single Domain)
/// Pattern: ProviderBasedRepository với Provider pattern
/// Use case: Service đơn giản chỉ cần 1 domain (User)
/// </summary>
public class SimpleUserService : ProviderBasedRepository<User>
{
    // ✅ Khai báo DAO name sử dụng từ Provider
    private const string DAO_NAME = Provider.DAO001; // User Management

    public SimpleUserService(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider,
        ILogger<SimpleUserService> logger)
        : base(sessionFactory, provider, DAO_NAME, logger: logger)
    {
        // Provider tự động resolve:
        // Provider.DAO001 → "SqlMaps/DAO001.xml"
        // Chỉ statements từ DAO001.xml available
    }

    #region User Management Methods

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        Logger?.LogInformation("Getting all users");
        return await QueryListAsync("User.GetAllUsers");
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        Logger?.LogInformation("Getting user {UserId}", id);
        return await QuerySingleAsync("User.GetUserById", new { Id = id });
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        Logger?.LogInformation("Getting user by email {Email}", email);
        return await QuerySingleAsync("User.GetUserByEmail", new { Email = email });
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        Logger?.LogInformation("Getting active users");
        return await QueryListAsync("User.GetActiveUsers");
    }

    public async Task<int> CreateUserAsync(User user)
    {
        Logger?.LogInformation("Creating user {Email}", user.Email);
        return await ExecuteAsync("User.InsertUser", user);
    }

    public async Task<int> UpdateUserAsync(User user)
    {
        Logger?.LogInformation("Updating user {UserId}", user.Id);
        return await ExecuteAsync("User.UpdateUser", user);
    }

    public async Task<int> DeleteUserAsync(int id)
    {
        Logger?.LogInformation("Deleting user {UserId}", id);
        return await ExecuteAsync("User.DeleteUser", new { Id = id });
    }

    public async Task<int> ActivateUserAsync(int id)
    {
        Logger?.LogInformation("Activating user {UserId}", id);
        return await ExecuteAsync("User.ActivateUser", new { Id = id });
    }

    public async Task<int> DeactivateUserAsync(int id)
    {
        Logger?.LogInformation("Deactivating user {UserId}", id);
        return await ExecuteAsync("User.DeactivateUser", new { Id = id });
    }

    #endregion

    #region Business Logic

    /// <summary>
    /// Business logic: Register new user with validation
    /// </summary>
    public async Task<(bool Success, string Message, int UserId)> RegisterUserAsync(
        string name, string email, string password)
    {
        // Check if email exists
        var existingUser = await GetUserByEmailAsync(email);
        if (existingUser != null)
        {
            return (false, "Email already exists", 0);
        }

        // Create new user
        var user = new User
        {
            Name = name,
            Email = email,
            Password = password, // In production: hash this!
            IsActive = true
        };

        try
        {
            var userId = await CreateUserAsync(user);
            Logger?.LogInformation("User registered successfully: {Email} (ID: {UserId})", email, userId);
            return (true, "User registered successfully", userId);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to register user {Email}", email);
            return (false, $"Registration failed: {ex.Message}", 0);
        }
    }

    /// <summary>
    /// Business logic: Update user profile
    /// </summary>
    public async Task<bool> UpdateUserProfileAsync(int userId, string name, string email)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null)
        {
            Logger?.LogWarning("User {UserId} not found for profile update", userId);
            return false;
        }

        // Check if new email is taken by another user
        if (user.Email != email)
        {
            var emailTaken = await GetUserByEmailAsync(email);
            if (emailTaken != null && emailTaken.Id != userId)
            {
                Logger?.LogWarning("Email {Email} is already taken", email);
                return false;
            }
        }

        user.Name = name;
        user.Email = email;

        var rowsAffected = await UpdateUserAsync(user);
        return rowsAffected > 0;
    }

    #endregion
}
