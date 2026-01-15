using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using ŠišAppApi.Data;
using ŠišAppApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ŠišAppApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;

    public PaymentController(IConfiguration configuration, ApplicationDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    [HttpPost("create-checkout-session")]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CheckoutSessionRequest request)
    {
        try
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = request.Amount, // Cijena u centima
                            Currency = "eur",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = request.ServiceName,
                                Description = request.ServiceDescription
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = request.SuccessUrl,
                CancelUrl = request.CancelUrl,
                CustomerEmail = request.CustomerEmail,
                Metadata = new Dictionary<string, string>
                {
                    { "appointmentId", request.AppointmentId?.ToString() ?? "" },
                    { "customerId", request.CustomerId?.ToString() ?? "" }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return Ok(new { sessionId = session.Id, url = session.Url });
        }
        catch (StripeException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Greška prilikom kreiranja checkout sesije: {ex.Message}" });
        }
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var endpointSecret = _configuration["Stripe:WebhookSecret"];

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                endpointSecret ?? throw new InvalidOperationException("Stripe webhook secret is not configured")
            );

            // Handle the event
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    var session = stripeEvent.Data.Object as Session;
                    if (session != null)
                    {
                        // Handle successful payment
                        Console.WriteLine($"Payment successful for session {session.Id}");
                        if (session.Metadata.TryGetValue("appointmentId", out var appointmentIdStr) && 
                            int.TryParse(appointmentIdStr, out var appointmentId))
                        {
                             // We need to resolve DbContext here since we might lose scope, 
                             // but Controller is Scoped so it's fine.
                             // Need to inject DbContext constructor first
                             // Assuming we add _context to PaymentController
                             var appointment = await _context.Appointments.FindAsync(appointmentId);
                             if (appointment != null)
                             {
                                 appointment.PaymentStatus = "Paid";
                                 // Add Payment Record
                                 var payment = new Payment
                                 {
                                     AppointmentId = appointmentId,
                                     UserId = appointment.UserId,
                                     Amount = (decimal)(session.AmountTotal ?? 0) / 100m,
                                     Currency = session.Currency,
                                     Method = "Stripe",
                                     TransactionId = session.PaymentIntentId ?? session.Id,
                                     Status = "Completed",
                                     CreatedAt = DateTime.UtcNow
                                 };
                                 _context.Payments.Add(payment);
                                 await _context.SaveChangesAsync();
                             }
                        }
                    }
                    break;
                case "payment_intent.succeeded":
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (paymentIntent != null)
                    {
                        // Handle successful payment intent
                        Console.WriteLine($"Payment intent succeeded: {paymentIntent.Id}");
                    }
                    break;
                default:
                    Console.WriteLine($"Unhandled event type: {stripeEvent.Type}");
                    break;
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Greška prilikom obrade webhook-a: {ex.Message}" });
        }
    }
}

public class CheckoutSessionRequest
{
    public long Amount { get; set; }

    [Required]
    public string ServiceName { get; set; } = string.Empty;

    [Required]
    public string ServiceDescription { get; set; } = string.Empty;

    [Required]
    public string SuccessUrl { get; set; } = string.Empty;

    [Required]
    public string CancelUrl { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string CustomerEmail { get; set; } = string.Empty;

    public int? AppointmentId { get; set; }
    public int? CustomerId { get; set; }
} 