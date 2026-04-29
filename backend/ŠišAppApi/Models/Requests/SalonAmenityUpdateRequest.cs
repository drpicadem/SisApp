namespace ŠišAppApi.Models.Requests;

public class SalonAmenityUpdateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageId { get; set; }
    public bool IsAvailable { get; set; }
    public int DisplayOrder { get; set; }
}
