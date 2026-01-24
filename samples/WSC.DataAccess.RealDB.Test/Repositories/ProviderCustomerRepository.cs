using WSC.DataAccess.Configuration;
using WSC.DataAccess.Core;
using WSC.DataAccess.Repository;

namespace WSC.DataAccess.RealDB.Test.Repositories;

/// <summary>
/// Example: Customer repository với provider pattern
/// </summary>
public class ProviderCustomerRepository : ProviderBasedRepository<dynamic>
{
    private const string MAP_KEY = "Customer";

    public ProviderCustomerRepository(
        IDbSessionFactory sessionFactory,
        SqlMapProvider provider)
        : base(sessionFactory, provider, MAP_KEY)
    {
    }

    public async Task<IEnumerable<dynamic>> GetAllCustomersAsync()
    {
        return await QueryListAsync("Customer.GetAll");
    }

    public async Task<dynamic?> GetByIdAsync(int id)
    {
        return await QuerySingleAsync("Customer.GetById", new { Id = id });
    }
}
