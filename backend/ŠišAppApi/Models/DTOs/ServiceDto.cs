namespace ŠišAppApi.Models.DTOs
{
    public class ServiceDto
    {
        public int Id { get; set; }
        public int SalonId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int DurationMinutes { get; set; }
        public bool IsPopular { get; set; }
        public bool IsActive { get; set; }
    }
}
