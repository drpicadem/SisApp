namespace ŠišAppApi.Models.SearchObjects
{
    public class UserSearchObject
    {
        public string? Q { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public int? SalonId { get; set; }
        public string? Role { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public bool? IsDeleted { get; set; }
    }
}
