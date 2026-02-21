namespace ŠišAppApi.Models.DTOs
{
    public class AppointmentDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public UserDto User { get; set; }
        
        public int ServiceId { get; set; }
        public ServiceDto Service { get; set; }
        
        public int BarberId { get; set; }
        public BarberDto Barber { get; set; }
        
        public int SalonId { get; set; }
        public SalonDto Salon { get; set; }
        
        public DateTime AppointmentDateTime { get; set; }
        public string Status { get; set; }
        public string PaymentStatus { get; set; }
        public string? Notes { get; set; }
        public string? CustomerNotes { get; set; }
        public string? BarberNotes { get; set; }
    }
}
