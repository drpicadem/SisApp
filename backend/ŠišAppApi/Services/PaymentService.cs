using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using ŠišAppApi.Constants;
using ŠišAppApi.Controllers;
using ŠišAppApi.Data;

namespace ŠišAppApi.Services;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IPaymentFinalizationService _paymentFinalizationService;

    public PaymentService(
        ApplicationDbContext context,
        IPaymentFinalizationService paymentFinalizationService)
    {
        _context = context;
        _paymentFinalizationService = paymentFinalizationService;
    }

    public async Task<IActionResult> CancelPending(CancelPendingStripeRequest request)
    {
        if (!request.AppointmentId.HasValue)
        {
            return new BadRequestObjectResult(new { error = "AppointmentId je obavezan." });
        }

        var appointment = await _context.Appointments.FindAsync(request.AppointmentId.Value);
        if (appointment == null)
        {
            return new NotFoundObjectResult(new { error = "Appointment not found" });
        }

        if (appointment.PaymentStatus == AppointmentPaymentStatuses.Paid)
        {
            return new OkObjectResult(new { status = "AlreadyPaid" });
        }

        var pending = await _context.Payments
            .FirstOrDefaultAsync(p => p.AppointmentId == request.AppointmentId.Value
                                      && p.Status == PaymentStatuses.Pending
                                      && p.Method == PaymentMethods.Stripe);
        if (pending == null)
        {
            return new OkObjectResult(new { status = "NoPending" });
        }

        pending.Status = PaymentStatuses.Cancelled;
        pending.UpdatedAt = DateTime.UtcNow;
        appointment.PaymentIntentId = null;
        appointment.StripeSessionId = null;
        await _context.SaveChangesAsync();

        return new OkObjectResult(new { status = "Cancelled" });
    }

    public async Task<IActionResult> HandleWebhook(string jsonPayload, string stripeSignature, string? webhookSecret)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                jsonPayload,
                stripeSignature,
                webhookSecret ?? throw new InvalidOperationException("Stripe webhook secret is not configured")
            );

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    if (stripeEvent.Data.Object is Session session &&
                        session.Metadata.TryGetValue("appointmentId", out var sessAppId) &&
                        int.TryParse(sessAppId, out var sessAppointmentId))
                    {
                        await _paymentFinalizationService.FinalizeAsync(new PaymentFinalizationInput
                        {
                            AppointmentId = sessAppointmentId,
                            Amount = (decimal)(session.AmountTotal ?? 0) / 100m,
                            Currency = session.Currency,
                            Method = PaymentMethods.Stripe,
                            TransactionId = session.PaymentIntentId ?? session.Id
                        });
                    }
                    break;

                case "payment_intent.succeeded":
                    if (stripeEvent.Data.Object is PaymentIntent paymentIntent &&
                        paymentIntent.Metadata.TryGetValue("appointmentId", out var piAppId) &&
                        int.TryParse(piAppId, out var piAppointmentId))
                    {
                        await _paymentFinalizationService.FinalizeAsync(new PaymentFinalizationInput
                        {
                            AppointmentId = piAppointmentId,
                            Amount = (decimal)paymentIntent.Amount / 100m,
                            Currency = paymentIntent.Currency,
                            Method = PaymentMethods.Stripe,
                            TransactionId = paymentIntent.Id
                        });
                    }
                    break;
            }

            return new OkResult();
        }
        catch (StripeException)
        {
            return new BadRequestObjectResult(new { error = "Webhook validacija nije uspjela." });
        }
        catch (Exception)
        {
            return new ObjectResult(new { error = "Došlo je do serverske greške prilikom obrade webhook-a." })
            {
                StatusCode = 500
            };
        }
    }
}
