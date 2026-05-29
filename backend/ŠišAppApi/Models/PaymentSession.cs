using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ŠišAppApi.Models;

public class PaymentSession
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [Required]
    public int AppointmentId { get; set; }

    [Required]
    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; } = false;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
