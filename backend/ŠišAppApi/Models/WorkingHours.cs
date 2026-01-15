using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ŠišAppApi.Models;

public class WorkingHours
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int BarberId { get; set; }
    
    [ForeignKey("BarberId")]
    public Barber Barber { get; set; } = null!;
    
    [Required]
    public int DayOfWeek { get; set; } // 0 = Sunday, 6 = Saturday
    
    [Required]
    public TimeSpan StartTime { get; set; }
    
    [Required]
    public TimeSpan EndTime { get; set; }
    
    public bool IsWorking { get; set; } = true;
    
    public bool IsDefault { get; set; } = false;
    
    public DateTime? ValidFrom { get; set; }
    
    public DateTime? ValidTo { get; set; }
    
    public string? Notes { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
} 