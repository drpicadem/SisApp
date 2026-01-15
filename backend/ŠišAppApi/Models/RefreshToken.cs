using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ŠišAppApi.Models;

public class RefreshToken
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
    
    [Required]
    [MaxLength(100)]
    public string Token { get; set; } = string.Empty;
    
    [Required]
    public DateTime ExpiresAt { get; set; }
    
    public DateTime? RevokedAt { get; set; }
    
    [MaxLength(50)]
    public string? ReplacedByToken { get; set; }
    
    [MaxLength(255)]
    public string? ReasonRevoked { get; set; }
    
    [MaxLength(50)]
    public string? DeviceId { get; set; }
    
    [MaxLength(255)]
    public string? UserAgent { get; set; }
    
    [MaxLength(50)]
    public string? IpAddress { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
} 