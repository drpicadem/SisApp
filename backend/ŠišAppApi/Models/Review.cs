using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ŠišAppApi.Models;

public class Review
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
    
    [Required]
    public int BarberId { get; set; }
    
    [ForeignKey("BarberId")]
    public Barber Barber { get; set; } = null!;
    
    [Required]
    public int AppointmentId { get; set; }
    
    [ForeignKey("AppointmentId")]
    public Appointment Appointment { get; set; } = null!;
    
    public int? SalonId { get; set; }
    
    [ForeignKey("SalonId")]
    public Salon? Salon { get; set; }
    
    public int? CustomerId { get; set; }
    
    [ForeignKey("CustomerId")]
    public Customer? Customer { get; set; }
    
    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }
    
    public string? Comment { get; set; }
    
    public string? ImageIds { get; set; } // JSON array of image IDs
    
    public bool IsVerified { get; set; } = false;
    
    public bool IsHidden { get; set; } = false;
    
    public int HelpfulCount { get; set; } = 0;
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
} 