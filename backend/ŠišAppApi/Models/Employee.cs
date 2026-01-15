using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ŠišAppApi.Models;

public class Employee
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
    
    [Required]
    [MaxLength(50)]
    public string Position { get; set; } = string.Empty;
    
    public DateTime? HireDate { get; set; }
    
    public DateTime? TerminationDate { get; set; }
    
    public string? EmploymentDetails { get; set; } // JSON
    
    public string? Schedule { get; set; } // JSON
    
    public string? Permissions { get; set; } // JSON
    
    public bool IsActive { get; set; } = true;
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public virtual ICollection<WorkingHours> WorkingHours { get; set; } = new List<WorkingHours>();
} 