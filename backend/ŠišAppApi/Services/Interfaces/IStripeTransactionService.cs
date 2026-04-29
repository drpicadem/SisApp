using ŠišAppApi.Models.DTOs;

namespace ŠišAppApi.Services.Interfaces
{
    public interface IStripeTransactionService
    {
        Task<AppointmentForPaymentDto?> GetAppointmentForPaymentAsync(int appointmentId, int userId);
        Task<StripePaymentIntentData> CreatePaymentIntentAsync(int appointmentId, long amountInCents);
        Task<StripeCompletePurchaseResult> CompletePurchaseAsync(int appointmentId, int userId, string? paymentIntentId);
    }
}
