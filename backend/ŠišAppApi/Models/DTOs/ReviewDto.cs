namespace ŠišAppApi.Models.DTOs;

public class ReviewDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int BarberId { get; set; }
    public string BarberName { get; set; } = string.Empty;
    public int? SalonId { get; set; }
    public string? SalonName { get; set; }
    public int AppointmentId { get; set; }
    public string? ServiceName { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int HelpfulCount { get; set; }
    public bool IsVerified { get; set; }
    public string? BarberResponse { get; set; }
    public DateTime? BarberRespondedAt { get; set; }
}