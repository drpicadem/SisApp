using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ŠišAppApi.Models;

public class Recommendation
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
    
    [Required]
    public int RecommendedServiceId { get; set; }
    
    [ForeignKey("RecommendedServiceId")]
    public Service RecommendedService { get; set; } = null!;
    
    [Required]
    [MaxLength(100)]
    public string Reason { get; set; } = string.Empty; // e.g. based on previous use, high rating
    
    public float? RelevanceScore { get; set; }
    
    public bool IsViewed { get; set; } = false;
    
    public DateTime? ViewedAt { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
} 