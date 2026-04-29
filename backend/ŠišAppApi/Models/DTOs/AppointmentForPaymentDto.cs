namespace ŠišAppApi.Models.DTOs
{
    public class AppointmentForPaymentDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public bool AlreadyPaid { get; set; }
        public long AmountInCents { get; set; }
        public string? ExistingPaymentIntentId { get; set; }
    }

    public class StripePaymentIntentData
    {
        public string ClientSecret { get; set; } = string.Empty;
        public string PaymentIntentId { get; set; } = string.Empty;
        public long AmountInCents { get; set; }
    }

    public class StripeCompletePurchaseResult
    {
        public bool AlreadyCompleted { get; set; }
        public bool PaymentNotSucceeded { get; set; }
        public string? StripeStatus { get; set; }
        public bool AmountMismatch { get; set; }
    }
}
