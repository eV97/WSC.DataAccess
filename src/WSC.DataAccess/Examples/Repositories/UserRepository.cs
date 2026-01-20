using Dapper;
using WSC.DataAccess.Core;
using WSC.DataAccess.Examples.Models;
using WSC.DataAccess.Repository;

namespace WSC.DataAccess.Examples.Repositories;

/// <summary>
/// Example User repository using BaseRepository
/// </summary>
public class UserRepository : BaseRepository<User>
{
    public UserRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory, "Users", "Id")
    {
    }

    public override async Task<int> InsertAsync(User entity)
    {
        var sql = @"
            INSERT INTO Users (Username, Email, FullName, CreatedDate, IsActive)
            VALUES (@Username, @Email, @FullName, @CreatedDate, @IsActive);
            SELECT CAST(SCOPE_IDENTITY() as int)";

        using var session = SessionFactory.OpenSession();
        return await session.Connection.ExecuteScalarAsync<int>(sql, entity);
    }

    public override async Task<int> UpdateAsync(User entity)
    {
        var sql = @"
            UPDATE Users
            SET Username = @Username,
                Email = @Email,
                FullName = @FullName,
                LastLoginDate = @LastLoginDate,
                IsActive = @IsActive
            WHERE Id = @Id";

        using var session = SessionFactory.OpenSession();
        return await session.Connection.ExecuteAsync(sql, entity);
    }

    /// <summary>
    /// Gets user by username
    /// </summary>
    public async Task<User?> GetByUsernameAsync(string username)
    {
        var sql = "SELECT * FROM Users WHERE Username = @Username";
        using var session = SessionFactory.OpenSession();
        return await session.Connection.QueryFirstOrDefaultAsync<User>(sql, new { Username = username });
    }

    /// <summary>
    /// Gets active users
    /// </summary>
    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        var sql = "SELECT * FROM Users WHERE IsActive = 1 ORDER BY Username";
        return await QueryAsync(sql);
    }

    /// <summary>
    /// Updates last login date
    /// </summary>
    public async Task<int> UpdateLastLoginAsync(int userId, DateTime loginDate)
    {
        var sql = "UPDATE Users SET LastLoginDate = @LoginDate WHERE Id = @UserId";
        return await ExecuteAsync(sql, new { UserId = userId, LoginDate = loginDate });
    }
}
