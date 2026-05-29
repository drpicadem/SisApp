using ŠišAppApi.Models.DTOs;

namespace ŠišAppApi.Services.Interfaces
{
    public interface IPayPalOrderService
    {
        Task<string> CreateOrderAsync(int appointmentId, int userId);
        Task<PayPalCaptureResult> CaptureOrderAsync(string orderId, int appointmentId, int userId);
        Task<PayPalCancelResult> CancelPendingAsync(int appointmentId, int userId);
    }
}
