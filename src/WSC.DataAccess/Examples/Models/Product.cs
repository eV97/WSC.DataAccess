using System.ComponentModel.DataAnnotations.Schema;

namespace WSC.DataAccess.Examples.Models;

/// <summary>
/// Example Product model
/// </summary>
[Table("Products")]
public class Product
{
    public int Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public bool IsActive { get; set; }
}
