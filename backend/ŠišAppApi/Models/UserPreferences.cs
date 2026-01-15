using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ŠišAppApi.Models;

public class UserPreferences
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
    
    [MaxLength(10)]
    public string? Language { get; set; } = "hr";
    
    [MaxLength(10)]
    public string? Currency { get; set; } = "EUR";
    
    [MaxLength(20)]
    public string? TimeZone { get; set; } = "Europe/Zagreb";
    
    public bool EmailNotifications { get; set; } = true;
    
    public bool SmsNotifications { get; set; } = true;
    
    public bool PushNotifications { get; set; } = true;
    
    public string? NotificationPreferences { get; set; } // JSON
    
    public string? Theme { get; set; } = "light";
    
    public string? DisplaySettings { get; set; } // JSON
    
    public string? PrivacySettings { get; set; } // JSON
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
} 