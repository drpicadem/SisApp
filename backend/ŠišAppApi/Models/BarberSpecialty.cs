using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ŠišAppApi.Models;

public class BarberSpecialty
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int BarberId { get; set; }
    
    [ForeignKey("BarberId")]
    public Barber Barber { get; set; } = null!;
    
    [Required]
    public int ServiceId { get; set; }
    
    [ForeignKey("ServiceId")]
    public Service Service { get; set; } = null!;
    
    [Required]
    [Range(1, 5)]
    public int ExpertiseLevel { get; set; } = 3;
    
    public string? Notes { get; set; }
    
    public bool IsPrimary { get; set; } = false;
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
} 