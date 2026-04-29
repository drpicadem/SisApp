namespace ŠišAppApi.Models.DTOs
{
    public class PayPalCaptureResult
    {
        public bool AlreadyPaid { get; set; }
        public bool AppointmentNotFound { get; set; }
        public string? CaptureId { get; set; }
    }

    public class PayPalCancelResult
    {
        public bool HadPending { get; set; }
    }
}
