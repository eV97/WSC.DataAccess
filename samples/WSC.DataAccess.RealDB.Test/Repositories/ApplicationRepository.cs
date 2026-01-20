using WSC.DataAccess.Core;
using WSC.DataAccess.Mapping;
using WSC.DataAccess.Repository;
using WSC.DataAccess.RealDB.Test.Models;

namespace WSC.DataAccess.RealDB.Test.Repositories;

/// <summary>
/// Repository cho Application sử dụng SqlMapRepository (IBatis-style)
/// </summary>
public class ApplicationRepository : SqlMapRepository<Application>
{
    public ApplicationRepository(IDbSessionFactory sessionFactory, SqlMapper sqlMapper)
        : base(sessionFactory, sqlMapper)
    {
    }

    /// <summary>
    /// Lấy tất cả applications - sử dụng SQL từ XML
    /// </summary>
    public async Task<IEnumerable<Application>> GetAllApplicationsAsync()
    {
        return await QueryListAsync("Application.GetAll");
    }

    /// <summary>
    /// Lấy application theo ID - sử dụng SQL từ XML
    /// </summary>
    public async Task<Application?> GetByIdAsync(int id)
    {
        return await QuerySingleAsync("Application.GetById", new { Id = id });
    }

    /// <summary>
    /// Tìm kiếm applications theo tên - sử dụng SQL từ XML
    /// </summary>
    public async Task<IEnumerable<Application>> SearchByNameAsync(string searchTerm)
    {
        return await QueryListAsync("Application.SearchByName",
            new { SearchTerm = $"%{searchTerm}%" });
    }

    /// <summary>
    /// Thêm application mới - sử dụng SQL từ XML
    /// </summary>
    public async Task<int> InsertAsync(Application application)
    {
        return await ExecuteAsync("Application.Insert", application);
    }

    /// <summary>
    /// Cập nhật application - sử dụng SQL từ XML
    /// </summary>
    public async Task<int> UpdateAsync(Application application)
    {
        return await ExecuteAsync("Application.Update", application);
    }

    /// <summary>
    /// Xóa application (soft delete) - sử dụng SQL từ XML
    /// </summary>
    public async Task<int> DeleteAsync(int id)
    {
        return await ExecuteAsync("Application.Delete", new { Id = id });
    }

    /// <summary>
    /// Đếm số lượng active applications - sử dụng SQL từ XML
    /// </summary>
    public async Task<int> GetActiveCountAsync()
    {
        using var session = SessionFactory.OpenSession();
        return await SqlMapper.ExecuteScalarAsync<int>(session, "Application.GetActiveCount", null);
    }
}
