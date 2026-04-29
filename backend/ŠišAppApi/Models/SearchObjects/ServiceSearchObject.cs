namespace ŠišAppApi.Models.SearchObjects
{
    public class ServiceSearchObject
    {
        public string? Q { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public int? SalonId { get; set; }
        public string? Name { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
    }
}
