namespace ŠišAppApi.Models.SearchObjects;

public class SalonAmenitySearchObject
{
    public string? Q { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public int? SalonId { get; set; }
    public string? Name { get; set; }
    public bool? IsAvailable { get; set; }
    public bool? IsDeleted { get; set; }
}
