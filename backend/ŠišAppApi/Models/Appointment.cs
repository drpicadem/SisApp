using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ŠišAppApi.Models;

public class Appointment
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
    
    [Required]
    public int ServiceId { get; set; }
    
    [ForeignKey("ServiceId")]
    public Service Service { get; set; } = null!;
    
    [Required]
    public int BarberId { get; set; }
    
    [ForeignKey("BarberId")]
    public Barber Barber { get; set; } = null!;
    
    [Required]
    public int SalonId { get; set; }
    
    [ForeignKey("SalonId")]
    public Salon Salon { get; set; } = null!;
    
    [Required]
    public DateTime AppointmentDateTime { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Cancelled, Completed
    
    [Required]
    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, Failed
    
    public string? Notes { get; set; }
    
    public string? CustomerNotes { get; set; }
    
    public string? BarberNotes { get; set; }
    
    public DateTime? ConfirmedAt { get; set; }
    
    public DateTime? CancelledAt { get; set; }
    
    public string? CancellationReason { get; set; }
    
    public bool IsNoShow { get; set; } = false;
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
    
    // Navigation properties
    public virtual Payment? Payment { get; set; }
    public virtual Review? Review { get; set; }
} 