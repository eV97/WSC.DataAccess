# Hướng dẫn Tạo Repository từ Đầu

Tutorial chi tiết về cách tạo một repository mới cho entity của bạn sử dụng WSC.DataAccess.

## Mục lục

1. [Tạo Model](#1-tạo-model)
2. [Tạo Repository với BaseRepository](#2-tạo-repository-với-baserepository)
3. [Tạo Repository với SqlMapRepository](#3-tạo-repository-với-sqlmaprepository)
4. [Đăng ký trong DI Container](#4-đăng-ký-trong-di-container)
5. [Sử dụng Repository](#5-sử-dụng-repository)
6. [Testing](#6-testing)

---

## 1. Tạo Model

### Bước 1.1: Tạo Model Class

Tạo file `Models/Customer.cs`:

```csharp
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApp.Models;

[Table("Customers")]
public class Customer
{
    public int Id { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public bool IsActive { get; set; }
}
```

### Bước 1.2: Tạo Database Table

```sql
CREATE TABLE [dbo].[Customers] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [CustomerCode] NVARCHAR(50) NOT NULL UNIQUE,
    [CompanyName] NVARCHAR(200) NOT NULL,
    [ContactName] NVARCHAR(100) NOT NULL,
    [Email] NVARCHAR(100) NOT NULL,
    [Phone] NVARCHAR(20) NULL,
    [Address] NVARCHAR(500) NULL,
    [City] NVARCHAR(100) NULL,
    [Country] NVARCHAR(100) NULL,
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
    [UpdatedDate] DATETIME2 NULL,
    [IsActive] BIT NOT NULL DEFAULT 1
);

-- Indexes
CREATE INDEX IX_Customers_CustomerCode ON [dbo].[Customers]([CustomerCode]);
CREATE INDEX IX_Customers_Email ON [dbo].[Customers]([Email]);
CREATE INDEX IX_Customers_CompanyName ON [dbo].[Customers]([CompanyName]);
```

---

## 2. Tạo Repository với BaseRepository

### Bước 2.1: Tạo Repository Class

Tạo file `Repositories/CustomerRepository.cs`:

```csharp
using Dapper;
using WSC.DataAccess.Core;
using WSC.DataAccess.Repository;
using MyApp.Models;

namespace MyApp.Repositories;

public class CustomerRepository : BaseRepository<Customer>
{
    public CustomerRepository(IDbSessionFactory sessionFactory)
        : base(sessionFactory, "Customers", "Id")
    {
    }

    #region Required Implementations

    public override async Task<int> InsertAsync(Customer entity)
    {
        var sql = @"
            INSERT INTO Customers (
                CustomerCode, CompanyName, ContactName, Email,
                Phone, Address, City, Country, CreatedDate, IsActive
            )
            VALUES (
                @CustomerCode, @CompanyName, @ContactName, @Email,
                @Phone, @Address, @City, @Country, @CreatedDate, @IsActive
            );
            SELECT CAST(SCOPE_IDENTITY() as int)";

        using var session = SessionFactory.OpenSession();
        return await session.Connection.ExecuteScalarAsync<int>(sql, entity);
    }

    public override async Task<int> UpdateAsync(Customer entity)
    {
        var sql = @"
            UPDATE Customers
            SET
                CustomerCode = @CustomerCode,
                CompanyName = @CompanyName,
                ContactName = @ContactName,
                Email = @Email,
                Phone = @Phone,
                Address = @Address,
                City = @City,
                Country = @Country,
                UpdatedDate = @UpdatedDate,
                IsActive = @IsActive
            WHERE Id = @Id";

        using var session = SessionFactory.OpenSession();
        return await session.Connection.ExecuteAsync(sql, entity);
    }

    #endregion

    #region Custom Methods

    /// <summary>
    /// Gets customer by customer code
    /// </summary>
    public async Task<Customer?> GetByCustomerCodeAsync(string customerCode)
    {
        var sql = "SELECT * FROM Customers WHERE CustomerCode = @CustomerCode";
        using var session = SessionFactory.OpenSession();
        return await session.Connection.QueryFirstOrDefaultAsync<Customer>(
            sql, new { CustomerCode = customerCode });
    }

    /// <summary>
    /// Searches customers by company name
    /// </summary>
    public async Task<IEnumerable<Customer>> SearchByCompanyAsync(string keyword)
    {
        var sql = @"
            SELECT * FROM Customers
            WHERE CompanyName LIKE @Keyword
              AND IsActive = 1
            ORDER BY CompanyName";

        return await QueryAsync(sql, new { Keyword = $"%{keyword}%" });
    }

    /// <summary>
    /// Gets customers by country
    /// </summary>
    public async Task<IEnumerable<Customer>> GetByCountryAsync(string country)
    {
        var sql = @"
            SELECT * FROM Customers
            WHERE Country = @Country AND IsActive = 1
            ORDER BY CompanyName";

        return await QueryAsync(sql, new { Country = country });
    }

    /// <summary>
    /// Gets active customers
    /// </summary>
    public async Task<IEnumerable<Customer>> GetActiveCustomersAsync()
    {
        var sql = @"
            SELECT * FROM Customers
            WHERE IsActive = 1
            ORDER BY CompanyName";

        return await QueryAsync(sql);
    }

    /// <summary>
    /// Deactivates a customer (soft delete)
    /// </summary>
    public async Task<int> DeactivateAsync(int customerId)
    {
        var sql = @"
            UPDATE Customers
            SET IsActive = 0, UpdatedDate = GETDATE()
            WHERE Id = @CustomerId";

        return await ExecuteAsync(sql, new { CustomerId = customerId });
    }

    #endregion
}
```

---

## 3. Tạo Repository với SqlMapRepository

### Bước 3.1: Tạo SQL Map XML

Tạo file `SqlMaps/CustomerMap.xml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<sqlMap namespace="Customer">

  <!-- Get all active customers -->
  <select id="Customer.GetAll" resultType="MyApp.Models.Customer">
    SELECT
      Id, CustomerCode, CompanyName, ContactName, Email,
      Phone, Address, City, Country, CreatedDate, UpdatedDate, IsActive
    FROM Customers
    WHERE IsActive = 1
    ORDER BY CompanyName
  </select>

  <!-- Get customer by ID -->
  <select id="Customer.GetById" resultType="MyApp.Models.Customer">
    SELECT
      Id, CustomerCode, CompanyName, ContactName, Email,
      Phone, Address, City, Country, CreatedDate, UpdatedDate, IsActive
    FROM Customers
    WHERE Id = @Id
  </select>

  <!-- Get customer by customer code -->
  <select id="Customer.GetByCode" resultType="MyApp.Models.Customer">
    SELECT
      Id, CustomerCode, CompanyName, ContactName, Email,
      Phone, Address, City, Country, CreatedDate, UpdatedDate, IsActive
    FROM Customers
    WHERE CustomerCode = @CustomerCode
  </select>

  <!-- Search customers by company name -->
  <select id="Customer.SearchByCompany" resultType="MyApp.Models.Customer">
    SELECT
      Id, CustomerCode, CompanyName, ContactName, Email,
      Phone, Address, City, Country, CreatedDate, UpdatedDate, IsActive
    FROM Customers
    WHERE CompanyName LIKE @Keyword
      AND IsActive = 1
    ORDER BY CompanyName
  </select>

  <!-- Get customers by country -->
  <select id="Customer.GetByCountry" resultType="MyApp.Models.Customer">
    SELECT
      Id, CustomerCode, CompanyName, ContactName, Email,
      Phone, Address, City, Country, CreatedDate, UpdatedDate, IsActive
    FROM Customers
    WHERE Country = @Country
      AND IsActive = 1
    ORDER BY CompanyName
  </select>

  <!-- Insert customer -->
  <insert id="Customer.Insert">
    INSERT INTO Customers (
      CustomerCode, CompanyName, ContactName, Email,
      Phone, Address, City, Country, CreatedDate, IsActive
    )
    VALUES (
      @CustomerCode, @CompanyName, @ContactName, @Email,
      @Phone, @Address, @City, @Country, @CreatedDate, @IsActive
    )
  </insert>

  <!-- Update customer -->
  <update id="Customer.Update">
    UPDATE Customers
    SET
      CustomerCode = @CustomerCode,
      CompanyName = @CompanyName,
      ContactName = @ContactName,
      Email = @Email,
      Phone = @Phone,
      Address = @Address,
      City = @City,
      Country = @Country,
      UpdatedDate = @UpdatedDate,
      IsActive = @IsActive
    WHERE Id = @Id
  </update>

  <!-- Soft delete customer -->
  <update id="Customer.Deactivate">
    UPDATE Customers
    SET IsActive = 0, UpdatedDate = GETDATE()
    WHERE Id = @Id
  </update>

  <!-- Hard delete customer -->
  <delete id="Customer.Delete">
    DELETE FROM Customers WHERE Id = @Id
  </delete>

</sqlMap>
```

### Bước 3.2: Tạo Repository Class

Tạo file `Repositories/CustomerRepository.cs`:

```csharp
using WSC.DataAccess.Core;
using WSC.DataAccess.Mapping;
using WSC.DataAccess.Repository;
using MyApp.Models;

namespace MyApp.Repositories;

public class CustomerRepository : SqlMapRepository<Customer>
{
    public CustomerRepository(IDbSessionFactory sessionFactory, SqlMapper sqlMapper)
        : base(sessionFactory, sqlMapper)
    {
    }

    public async Task<IEnumerable<Customer>> GetAllAsync()
    {
        return await QueryListAsync("Customer.GetAll");
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        return await QuerySingleAsync("Customer.GetById", new { Id = id });
    }

    public async Task<Customer?> GetByCustomerCodeAsync(string customerCode)
    {
        return await QuerySingleAsync("Customer.GetByCode",
            new { CustomerCode = customerCode });
    }

    public async Task<IEnumerable<Customer>> SearchByCompanyAsync(string keyword)
    {
        return await QueryListAsync("Customer.SearchByCompany",
            new { Keyword = $"%{keyword}%" });
    }

    public async Task<IEnumerable<Customer>> GetByCountryAsync(string country)
    {
        return await QueryListAsync("Customer.GetByCountry",
            new { Country = country });
    }

    public async Task<int> InsertAsync(Customer customer)
    {
        return await ExecuteAsync("Customer.Insert", customer);
    }

    public async Task<int> UpdateAsync(Customer customer)
    {
        customer.UpdatedDate = DateTime.Now;
        return await ExecuteAsync("Customer.Update", customer);
    }

    public async Task<int> DeactivateAsync(int id)
    {
        return await ExecuteAsync("Customer.Deactivate", new { Id = id });
    }

    public async Task<int> DeleteAsync(int id)
    {
        return await ExecuteAsync("Customer.Delete", new { Id = id });
    }
}
```

---

## 4. Đăng ký trong DI Container

### Bước 4.1: Cấu hình appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyDb;User Id=sa;Password=Pass123;TrustServerCertificate=True;"
  }
}
```

### Bước 4.2: Đăng ký trong Program.cs

```csharp
using Microsoft.Extensions.DependencyInjection;
using WSC.DataAccess.Configuration;
using MyApp.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Lấy connection string
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found");

// Đăng ký WSC Data Access
builder.Services.AddWscDataAccess(connectionString, options =>
{
    // Nếu dùng SqlMapRepository, cần đăng ký XML file
    options.AddSqlMapFile("SqlMaps/CustomerMap.xml");
});

// Đăng ký repository
builder.Services.AddScoped<CustomerRepository>();

var app = builder.Build();
```

---

## 5. Sử dụng Repository

### Bước 5.1: Trong Controller (Web API)

```csharp
using Microsoft.AspNetCore.Mvc;
using MyApp.Models;
using MyApp.Repositories;

namespace MyApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly CustomerRepository _customerRepository;

    public CustomersController(CustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Customer>>> GetAll()
    {
        var customers = await _customerRepository.GetAllAsync();
        return Ok(customers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Customer>> GetById(int id)
    {
        var customer = await _customerRepository.GetByIdAsync(id);
        if (customer == null)
            return NotFound();

        return Ok(customer);
    }

    [HttpGet("code/{code}")]
    public async Task<ActionResult<Customer>> GetByCode(string code)
    {
        var customer = await _customerRepository.GetByCustomerCodeAsync(code);
        if (customer == null)
            return NotFound();

        return Ok(customer);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<Customer>>> Search(
        [FromQuery] string keyword)
    {
        var customers = await _customerRepository.SearchByCompanyAsync(keyword);
        return Ok(customers);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create(Customer customer)
    {
        customer.CreatedDate = DateTime.Now;
        customer.IsActive = true;

        var customerId = await _customerRepository.InsertAsync(customer);
        return CreatedAtAction(nameof(GetById), new { id = customerId }, customerId);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, Customer customer)
    {
        customer.Id = id;
        customer.UpdatedDate = DateTime.Now;

        var result = await _customerRepository.UpdateAsync(customer);
        if (result == 0)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var result = await _customerRepository.DeactivateAsync(id);
        if (result == 0)
            return NotFound();

        return NoContent();
    }
}
```

### Bước 5.2: Trong Service Layer

```csharp
using MyApp.Models;
using MyApp.Repositories;

namespace MyApp.Services;

public class CustomerService
{
    private readonly CustomerRepository _customerRepository;

    public CustomerService(CustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Customer?> GetCustomerAsync(int id)
    {
        return await _customerRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Customer>> GetActiveCustomersAsync()
    {
        return await _customerRepository.GetActiveCustomersAsync();
    }

    public async Task<int> CreateCustomerAsync(Customer customer)
    {
        // Business logic validation
        if (string.IsNullOrWhiteSpace(customer.CustomerCode))
            throw new ArgumentException("Customer code is required");

        if (string.IsNullOrWhiteSpace(customer.Email))
            throw new ArgumentException("Email is required");

        // Check if customer code already exists
        var existing = await _customerRepository
            .GetByCustomerCodeAsync(customer.CustomerCode);

        if (existing != null)
            throw new InvalidOperationException("Customer code already exists");

        // Set defaults
        customer.CreatedDate = DateTime.Now;
        customer.IsActive = true;

        return await _customerRepository.InsertAsync(customer);
    }

    public async Task UpdateCustomerAsync(Customer customer)
    {
        var existing = await _customerRepository.GetByIdAsync(customer.Id);
        if (existing == null)
            throw new InvalidOperationException("Customer not found");

        customer.UpdatedDate = DateTime.Now;
        await _customerRepository.UpdateAsync(customer);
    }

    public async Task DeactivateCustomerAsync(int customerId)
    {
        await _customerRepository.DeactivateAsync(customerId);
    }
}
```

---

## 6. Testing

### Bước 6.1: Tạo Unit Test

```csharp
using Xunit;
using Moq;
using WSC.DataAccess.Core;
using MyApp.Models;
using MyApp.Repositories;

namespace MyApp.Tests.Repositories;

public class CustomerRepositoryTests
{
    private readonly Mock<IDbSessionFactory> _mockSessionFactory;
    private readonly CustomerRepository _repository;

    public CustomerRepositoryTests()
    {
        _mockSessionFactory = new Mock<IDbSessionFactory>();
        _repository = new CustomerRepository(_mockSessionFactory.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCustomer_WhenExists()
    {
        // Arrange
        var customerId = 1;
        var expectedCustomer = new Customer
        {
            Id = customerId,
            CustomerCode = "CUST001",
            CompanyName = "Test Company"
        };

        // Setup mock session
        // ... (implementation depends on your mocking strategy)

        // Act
        var result = await _repository.GetByIdAsync(customerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedCustomer.CustomerCode, result.CustomerCode);
    }
}
```

### Bước 6.2: Integration Test

```csharp
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using MyApp.Models;
using MyApp.Repositories;

namespace MyApp.Tests.Integration;

public class CustomerRepositoryIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly CustomerRepository _repository;

    public CustomerRepositoryIntegrationTests(DatabaseFixture fixture)
    {
        _repository = fixture.ServiceProvider
            .GetRequiredService<CustomerRepository>();
    }

    [Fact]
    public async Task CreateAndRetrieveCustomer_ShouldWork()
    {
        // Arrange
        var customer = new Customer
        {
            CustomerCode = $"TEST{DateTime.Now.Ticks}",
            CompanyName = "Test Company",
            ContactName = "John Doe",
            Email = "john@test.com",
            CreatedDate = DateTime.Now,
            IsActive = true
        };

        // Act - Create
        var customerId = await _repository.InsertAsync(customer);
        Assert.True(customerId > 0);

        // Act - Retrieve
        var retrieved = await _repository.GetByIdAsync(customerId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(customer.CustomerCode, retrieved.CustomerCode);
        Assert.Equal(customer.CompanyName, retrieved.CompanyName);
    }
}
```

---

## Checklist Tạo Repository Mới

- [ ] Tạo model class với attributes
- [ ] Tạo database table với indexes
- [ ] Chọn repository pattern (BaseRepository hoặc SqlMapRepository)
- [ ] Implement required methods (Insert, Update)
- [ ] Thêm custom methods theo business logic
- [ ] Tạo SQL Map XML (nếu dùng SqlMapRepository)
- [ ] Đăng ký repository trong DI container
- [ ] Đăng ký SQL map file (nếu dùng SqlMapRepository)
- [ ] Viết unit tests
- [ ] Viết integration tests
- [ ] Document public methods
- [ ] Review error handling
- [ ] Test với real database

---

## Tips và Best Practices

1. **Naming Convention**
   - Repository: `{Entity}Repository`
   - SQL Map ID: `{Entity}.{Action}`
   - Table: Số nhiều (Users, Products)

2. **Soft Delete**
   - Luôn ưu tiên soft delete (IsActive = 0)
   - Chỉ hard delete khi thực sự cần thiết

3. **Timestamps**
   - Luôn có CreatedDate
   - UpdatedDate cho tracking changes

4. **Indexes**
   - Index trên các columns thường query
   - Unique index cho business keys

5. **Validation**
   - Validate trong Service layer, không trong Repository
   - Repository chỉ lo về data access

6. **Error Handling**
   - Let exceptions bubble up
   - Log ở Service layer hoặc Controller

---

**Chúc bạn tạo repository thành công!** 🚀
