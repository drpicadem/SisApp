namespace ŠišAppApi.Models.DTOs
{
    public class ServiceDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Duration { get; set; } // Minutes
        public string? Currency { get; set; }
    }
}
