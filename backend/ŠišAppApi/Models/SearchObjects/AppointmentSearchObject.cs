namespace ŠišAppApi.Models.SearchObjects
{
    public class AppointmentSearchObject
    {
        public int? UserId { get; set; }
        public int? BarberId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Status { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsPaid { get; set; }
        public string? Q { get; set; }

        public int? Page { get; set; }
        public int? PageSize { get; set; }


        public int? CurrentUserId { get; set; }
        public string? CurrentUserRole { get; set; }
    }
}
