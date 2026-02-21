namespace ŠišAppApi.Models.DTOs
{
    public class SalonDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string? Phone { get; set; }
        public double Rating { get; set; }
    }
}
