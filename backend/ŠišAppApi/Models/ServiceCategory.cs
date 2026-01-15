using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ŠišAppApi.Models;

public class ServiceCategory
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public string? ImageId { get; set; }
    
    [ForeignKey("ImageId")]
    public Image? Image { get; set; }
    
    public int? ParentCategoryId { get; set; }
    
    [ForeignKey("ParentCategoryId")]
    public ServiceCategory? ParentCategory { get; set; }
    
    public int DisplayOrder { get; set; } = 0;
    
    public bool IsActive { get; set; } = true;
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<ServiceCategory> SubCategories { get; set; } = new List<ServiceCategory>();
    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
} 