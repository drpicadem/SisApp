using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ŠišAppApi.Models;

public class AdminLog
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int AdminId { get; set; }
    
    [ForeignKey("AdminId")]
    public User Admin { get; set; } = null!;
    
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;
    
    public int? EntityId { get; set; }
    
    public string? OldValues { get; set; } // JSON
    
    public string? NewValues { get; set; } // JSON
    
    [MaxLength(255)]
    public string? Notes { get; set; }
    
    [MaxLength(50)]
    public string? IpAddress { get; set; }
    
    [MaxLength(255)]
    public string? UserAgent { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
} 