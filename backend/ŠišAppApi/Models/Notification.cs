using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ŠišAppApi.Models;

public class Notification
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
    
    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = string.Empty; // Email, SMS, InApp
    
    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;
    
    public string? Data { get; set; } // JSON s dodatnim podacima
    
    public bool IsRead { get; set; } = false;
    
    public DateTime? ReadAt { get; set; }
    
    [Required]
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
} 