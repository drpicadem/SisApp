using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ŠišAppApi.Models;

public class FavoriteSalon
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public User? User { get; set; }

    [Required]
    public int SalonId { get; set; }

    [ForeignKey("SalonId")]
    public Salon? Salon { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
