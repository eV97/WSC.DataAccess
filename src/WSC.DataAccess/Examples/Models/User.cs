using System.ComponentModel.DataAnnotations.Schema;

namespace WSC.DataAccess.Examples.Models;

/// <summary>
/// Example User model
/// </summary>
[Table("Users")]
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public bool IsActive { get; set; }
}
