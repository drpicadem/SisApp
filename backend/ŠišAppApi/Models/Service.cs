using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ŠišAppApi.Models;

public class Service
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int SalonId { get; set; }
    
    [ForeignKey("SalonId")]
    public Salon Salon { get; set; } = null!;
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [Required]
    public int DurationMinutes { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }
    
    public int? CategoryId { get; set; }
    
    [ForeignKey("CategoryId")]
    public ServiceCategory? Category { get; set; }
    
    public string? ImageIds { get; set; } // JSON array of image IDs
    
    public bool IsPopular { get; set; } = false;
    
    public int DisplayOrder { get; set; } = 0;
    
    public bool IsActive { get; set; } = true;
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public virtual ICollection<BarberSpecialty> BarberSpecialties { get; set; } = new List<BarberSpecialty>();
    public virtual ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
} 