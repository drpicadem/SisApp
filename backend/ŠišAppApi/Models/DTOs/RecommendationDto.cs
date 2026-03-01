namespace ŠišAppApi.Models.DTOs;

public class RecommendationDto
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? ServiceDescription { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public int SalonId { get; set; }
    public string SalonName { get; set; } = string.Empty;
    public string SalonCity { get; set; } = string.Empty;
    public double SalonRating { get; set; }
    public string? SalonImageIds { get; set; }
    public string Reason { get; set; } = string.Empty;
    public float RelevanceScore { get; set; }
}
