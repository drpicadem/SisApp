namespace ŠišAppApi.Services;

public interface IPaymentFinalizationService
{
    Task<PaymentFinalizationResult> FinalizeAsync(PaymentFinalizationInput input);
}

public class PaymentFinalizationInput
{
    public int AppointmentId { get; set; }
    public string Method { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
}

public class PaymentFinalizationResult
{
    public bool AppointmentNotFound { get; set; }
    public bool AlreadyPaid { get; set; }
    public int? PaymentId { get; set; }
}
