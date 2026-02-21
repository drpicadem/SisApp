namespace ŠišAppApi.Models.DTOs
{
    public class BarberDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SalonId { get; set; }
        public UserDto User { get; set; }
        public string? Bio { get; set; }
        public double Rating { get; set; }
    }
}
