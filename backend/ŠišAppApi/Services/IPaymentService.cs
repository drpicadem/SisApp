using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Controllers;

namespace ŠišAppApi.Services;

public interface IPaymentService
{
    Task<IActionResult> CancelPending(CancelPendingStripeRequest request);
    Task<IActionResult> HandleWebhook(string jsonPayload, string stripeSignature, string? webhookSecret);
}
