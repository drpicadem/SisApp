using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ŠišAppApi.Models;

public class SalonAmenity
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
    
    public string? ImageId { get; set; }
    
    [ForeignKey("ImageId")]
    public Image? Image { get; set; }
    
    public bool IsAvailable { get; set; } = true;
    
    public int DisplayOrder { get; set; } = 0;
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
} 