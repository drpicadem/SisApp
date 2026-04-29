namespace ŠišAppApi.Models.DTOs;

public class SalonAmenityDto
{
    public int Id { get; set; }
    public int SalonId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageId { get; set; }
    public bool IsAvailable { get; set; }
    public int DisplayOrder { get; set; }
}
