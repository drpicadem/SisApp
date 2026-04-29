namespace ŠišAppApi.Models.DTOs
{
    public class BarberServiceItemDto
    {
        public int Id { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public decimal ServicePrice { get; set; }
        public int ServiceDuration { get; set; }
        public int ExpertiseLevel { get; set; }
        public bool IsPrimary { get; set; }
        public string? Notes { get; set; }
    }
}
