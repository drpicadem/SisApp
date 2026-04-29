using ŠišAppApi.Models.DTOs;

namespace ŠišAppApi.Services.Interfaces
{
    public interface IPayPalOrderService
    {
        Task<string> CreateOrderAsync(int appointmentId);
        Task<PayPalCaptureResult> CaptureOrderAsync(string orderId, int appointmentId);
        Task<PayPalCancelResult> CancelPendingAsync(int appointmentId);
    }
}
