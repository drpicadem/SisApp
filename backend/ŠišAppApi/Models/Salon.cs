using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ŠišAppApi.Models;

public class Salon
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Address { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string City { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string PostalCode { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Country { get; set; } = string.Empty;
    
    public double? Latitude { get; set; }
    
    public double? Longitude { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;
    
    [MaxLength(100)]
    [EmailAddress]
    public string? Email { get; set; }
    
    [MaxLength(255)]
    public string? Website { get; set; }
    
    public string? ImageIds { get; set; } // JSON array of image IDs
    
    [Required]
    [Range(0, 5)]
    public double Rating { get; set; } = 0;
    
    public int ReviewCount { get; set; } = 0;
    
    public bool IsVerified { get; set; } = false;
    
    public DateTime? VerifiedAt { get; set; }
    
    public string? VerificationNotes { get; set; }
    
    public string? BusinessHours { get; set; } // JSON object with business hours
    
    public string? Amenities { get; set; } // JSON array of amenities
    
    public string? SocialMedia { get; set; } // JSON object with social media links
    
    public bool IsActive { get; set; } = true;
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<Barber> Barbers { get; set; } = new List<Barber>();
    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
} 