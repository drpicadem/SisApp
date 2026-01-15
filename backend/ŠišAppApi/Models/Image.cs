using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ŠišAppApi.Models;

public class Image
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string ContentType { get; set; } = string.Empty;
    
    [Required]
    public long FileSize { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Url { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? ThumbnailUrl { get; set; }
    
    [MaxLength(255)]
    public string? AltText { get; set; }
    
    public int? Width { get; set; }
    
    public int? Height { get; set; }
    
    [MaxLength(50)]
    public string? ImageType { get; set; } // Profile, Gallery, Service, etc.
    
    public int? EntityId { get; set; } // ID of the entity this image belongs to
    
    [MaxLength(50)]
    public string? EntityType { get; set; } // Type of entity (User, Service, etc.)
    
    public int DisplayOrder { get; set; } = 0;
    
    public bool IsActive { get; set; } = true;
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
} 