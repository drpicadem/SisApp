using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ŠišAppApi.Models;

public class Customer
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
    
    public string? Preferences { get; set; } // JSON
    
    public string? FavoriteBarbers { get; set; } // JSON array of barber IDs
    
    public string? FavoriteSalons { get; set; } // JSON array of salon IDs
    
    public string? FavoriteServices { get; set; } // JSON array of service IDs
    
    public string? LoyaltyPoints { get; set; } // JSON
    
    public string? PaymentMethods { get; set; } // JSON array of payment methods
    
    public string? Addresses { get; set; } // JSON array of addresses
    
    public string? Notes { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
} 