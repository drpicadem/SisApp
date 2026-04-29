namespace ŠišAppApi.Models.SearchObjects;

public class ServiceCategorySearchObject
{
    public string? Q { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public string? Name { get; set; }
    public int? ParentCategoryId { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
}
