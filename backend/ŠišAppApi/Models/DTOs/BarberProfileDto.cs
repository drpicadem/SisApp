namespace ŠišAppApi.Models.DTOs
{
    public class BarberProfileDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SalonId { get; set; }
        public double Rating { get; set; }
        public string? Bio { get; set; }
        public string? ImageIds { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }
}
