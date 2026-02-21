namespace ŠišAppApi.Models.Requests
{
    public class AppointmentUpdateRequest
    {
        public string? Status { get; set; }
        public string? PaymentStatus { get; set; }
        public string? Note { get; set; }
    }
}
