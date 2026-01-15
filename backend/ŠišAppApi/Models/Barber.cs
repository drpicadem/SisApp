using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ŠišAppApi.Models;

public class Barber
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
    
    [Required]
    public int SalonId { get; set; }
    
    [ForeignKey("SalonId")]
    public Salon Salon { get; set; } = null!;
    
    public string? Bio { get; set; }
    
    public string? ImageIds { get; set; } // JSON array of image IDs
    
    [Required]
    [Range(0, 5)]
    public double Rating { get; set; } = 0;
    
    public int ReviewCount { get; set; } = 0;
    
    public int AppointmentCount { get; set; } = 0;
    
    public bool IsAvailable { get; set; } = true;
    
    public bool IsVerified { get; set; } = false;
    
    public DateTime? VerifiedAt { get; set; }
    
    public string? VerificationNotes { get; set; }
    
    public string? Skills { get; set; } // JSON array of skills
    
    public string? Certifications { get; set; } // JSON array of certifications
    
    public string? Languages { get; set; } // JSON array of languages
    
    public string? SocialMedia { get; set; } // JSON object with social media links
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<WorkingHours> WorkingHours { get; set; } = new List<WorkingHours>();
    public virtual ICollection<BarberSpecialty> Specialties { get; set; } = new List<BarberSpecialty>();
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
} 