using System.ComponentModel.DataAnnotations;

namespace ŠišAppApi.Models.Requests
{
    public class AppointmentInsertRequest
    {
        [Required]
        public DateTime AppointmentDateTime { get; set; }
        [Required]
        public int BarberId { get; set; }
        [Required]
        public int ServiceId { get; set; }
        [Required]
        public int SalonId { get; set; }
        public string? Note { get; set; }
    }
}
