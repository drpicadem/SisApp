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
    public User? User { get; set; }

    public string? Preferences { get; set; }

    public string? FavoriteBarbers { get; set; }

    public string? FavoriteSalons { get; set; }

    public string? FavoriteServices { get; set; }

    public string? LoyaltyPoints { get; set; }

    public string? PaymentMethods { get; set; }

    public string? Addresses { get; set; }

    public string? Notes { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }


    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
} 