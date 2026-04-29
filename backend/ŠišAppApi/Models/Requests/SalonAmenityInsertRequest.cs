namespace ŠišAppApi.Models.Requests;

public class SalonAmenityInsertRequest
{
    public int SalonId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageId { get; set; }
    public bool IsAvailable { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
}
