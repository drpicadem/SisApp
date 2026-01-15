using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ŠišAppApi.Models;

public class Admin
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
    
    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = "Admin"; // Admin, SuperAdmin
    
    public string? Permissions { get; set; } // JSON
    
    public bool IsSuperAdmin { get; set; } = false;
    
    public DateTime? LastLoginAt { get; set; }
    
    [MaxLength(50)]
    public string? LastLoginIp { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<AdminLog> AdminLogs { get; set; } = new List<AdminLog>();
} 