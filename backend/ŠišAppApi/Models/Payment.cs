using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ŠišAppApi.Models;

public class Payment
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int AppointmentId { get; set; }
    
    [ForeignKey("AppointmentId")]
    public Appointment Appointment { get; set; } = null!;
    
    [Required]
    public int UserId { get; set; }
    
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
    
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }
    
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "EUR";
    
    [Required]
    [MaxLength(20)]
    public string Method { get; set; } = string.Empty; // PayPal, Stripe, Cash
    
    [Required]
    [MaxLength(100)]
    public string TransactionId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Completed, Failed, Refunded
    
    public string? ReceiptUrl { get; set; }
    
    public string? RefundReason { get; set; }
    
    public DateTime? RefundedAt { get; set; }
    
    public string? PaymentDetails { get; set; } // JSON
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
} 