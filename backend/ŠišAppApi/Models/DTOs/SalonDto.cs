namespace ŠišAppApi.Models.DTOs
{
    public class SalonDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public int CityId { get; set; }
        public string City { get; set; }
        public string? Phone { get; set; }
        public double Rating { get; set; }
        public string? PostalCode { get; set; }
        public string? Website { get; set; }
        public string? ImageIds { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public List<string> Services { get; set; } = new List<string>();
        public int EmployeeCount { get; set; }
        public bool IsActive { get; set; }
    }
}
