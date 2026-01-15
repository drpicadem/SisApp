using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ŠišAppApi.Models;

public class User
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string PasswordHash { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
    
    public string? ImageId { get; set; }
    
    [ForeignKey("ImageId")]
    public Image? ProfileImage { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = "User"; // User, Barber, Admin
    
    public bool IsEmailVerified { get; set; } = false;
    
    public DateTime? EmailVerifiedAt { get; set; }
    
    public bool IsPhoneVerified { get; set; } = false;
    
    public DateTime? PhoneVerifiedAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime? LastLoginAt { get; set; }
    
    [MaxLength(50)]
    public string? LastLoginIp { get; set; }
    
    public string? Preferences { get; set; } // JSON
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
    
    // Navigation properties
    public virtual Barber? Barber { get; set; }
    public virtual Customer? Customer { get; set; }
    public virtual Admin? Admin { get; set; }
    public virtual Employee? Employee { get; set; }
    public virtual UserPreferences? UserPreferences { get; set; }
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public virtual ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
} 