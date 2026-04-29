namespace ŠišAppApi.Models.SearchObjects
{
    public class SalonSearchObject
    {
    public string? Q { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
        public string? Name { get; set; }
        public string? City { get; set; }
        public bool? IsActive { get; set; }
    }
}
