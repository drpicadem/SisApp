using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using ŠišAppApi.Data;
using ŠišAppApi.Models;
using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Services; // Added for IEmailService and INotificationService
using Microsoft.AspNetCore.Authorization;

namespace ŠišAppApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;

    public PaymentController(IConfiguration configuration, ApplicationDbContext context, IEmailService emailService, INotificationService notificationService)
    {
        _configuration = configuration;
        _context = context;
        _emailService = emailService;
        _notificationService = notificationService;
    }

    [HttpPost("create-checkout-session")]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CheckoutSessionRequest request)
    {
        try
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { (request.PaymentMethod?.ToLower() == "paypal") ? "paypal" : "card" },
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

            // Save Session ID to Appointment
            if (request.AppointmentId.HasValue)
            {
                var appointment = await _context.Appointments.FindAsync(request.AppointmentId.Value);
                if (appointment != null)
                {
                    appointment.StripeSessionId = session.Id;
                    await _context.SaveChangesAsync();
                }
            }

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

    [HttpGet("check-status/{appointmentId}")]
    public async Task<IActionResult> CheckPaymentStatus(int appointmentId)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment == null) return NotFound(new { error = "Appointment not found" });

        if (appointment.PaymentStatus == "Paid")
        {
            return Ok(new { status = "Paid" });
        }

        if (string.IsNullOrEmpty(appointment.StripeSessionId))
        {
             return Ok(new { status = "Pending" });
        }

        // Verify with Stripe
        try
        {
            var service = new SessionService();
            var session = await service.GetAsync(appointment.StripeSessionId);

            if (session.PaymentStatus == "paid")
            {
                appointment.PaymentStatus = "Paid";
                
                // Ensure Payment record exists
                var existingPayment = await _context.Payments.AnyAsync(p => p.AppointmentId == appointmentId);
                if (!existingPayment)
                {
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
                }

                await _context.SaveChangesAsync();

                // Send Notification and Email
                try
                {
                    var paymentRecord = await _context.Payments.FirstOrDefaultAsync(p => p.AppointmentId == appointmentId);
                    await _notificationService.CreateNotification(
                        appointment.UserId,
                        $"Uspješno ste platili rezervaciju termina za {appointment.AppointmentDateTime:dd.MM.yyyy HH:mm}.",
                        "Payment",
                        paymentRecord?.Id.ToString());

                    var user = await _context.Users.FindAsync(appointment.UserId);
                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        await _emailService.SendEmailAsync(user.Email, "Potvrda rezervacije - ŠišApp",
                            $"<h1>Rezervacija potvrđena!</h1><p>Poštovani/a {user.FirstName},</p><p>Vaša rezervacija za termin <strong>{appointment.AppointmentDateTime:dd.MM.yyyy HH:mm}</strong> je uspješno plaćena.</p><p>Hvala na povjerenju!</p>");
                    }
                }
                catch (Exception notifEx)
                {
                    Console.WriteLine($"Error sending notification/email from check-status: {notifEx.Message}");
                }

                return Ok(new { status = "Paid" });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking Stripe status: {ex.Message}");
        }

        return Ok(new { status = "Pending" });
    }

    [AllowAnonymous]
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

                                 // Send Notification and Email
                                 try 
                                 {
                                     await _notificationService.CreateNotification(appointment.UserId, $"Uspješno ste platili rezervaciju termina za {appointment.AppointmentDateTime:dd.MM.yyyy HH:mm}.", "Payment", payment.Id.ToString());
                                     
                                     // Fetch user email if not loaded
                                     var user = await _context.Users.FindAsync(appointment.UserId);
                                     if (user != null && !string.IsNullOrEmpty(user.Email))
                                     {
                                         await _emailService.SendEmailAsync(user.Email, "Potvrda rezervacije - ŠišApp", 
                                             $"<h1>Rezervacija potvrđena!</h1><p>Poštovani/a {user.FirstName},</p><p>Vaša rezervacija za termin <strong>{appointment.AppointmentDateTime:dd.MM.yyyy HH:mm}</strong> je uspješno plaćena.</p><p>Hvala na povjerenju!</p>");
                                     }
                                 }
                                 catch (Exception ex)
                                 {
                                     Console.WriteLine($"Error sending notification/email: {ex.Message}");
                                 }
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
    [AllowAnonymous]
    [HttpGet("success")]
    public ContentResult PaymentSuccess()
    {
        var html = @"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <title>Plaćanje Uspješno</title>
                <style>
                    body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; text-align: center; padding: 50px; background-color: #f4f4f9; }
                    .container { background: white; padding: 40px; border-radius: 10px; box-shadow: 0 4px 8px rgba(0,0,0,0.1); max-width: 500px; margin: auto; }
                    .success { color: #4CAF50; font-size: 2.5em; margin-bottom: 10px; }
                    p { color: #555; font-size: 1.1em; }
                    .icon { font-size: 4em; color: #4CAF50; margin-bottom: 20px; }
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='icon'>✅</div>
                    <h1 class='success'>Plaćanje Uspješno!</h1>
                    <p>Vaša rezervacija je potvrđena i plaćena.</p>
                    <p>Možete zatvoriti ovaj prozor i vratiti se u aplikaciju.</p>
                </div>
            </body>
            </html>";
            
        return new ContentResult
        {
            Content = html,
            ContentType = "text/html; charset=utf-8"
        };
    }

    [AllowAnonymous]
    [HttpGet("cancel")]
    public ContentResult PaymentCancel()
    {
        var html = @"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <title>Plaćanje Otkazano</title>
                <style>
                    body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; text-align: center; padding: 50px; background-color: #f4f4f9; }
                    .container { background: white; padding: 40px; border-radius: 10px; box-shadow: 0 4px 8px rgba(0,0,0,0.1); max-width: 500px; margin: auto; }
                    .error { color: #f44336; font-size: 2.5em; margin-bottom: 10px; }
                    p { color: #555; font-size: 1.1em; }
                    .icon { font-size: 4em; color: #f44336; margin-bottom: 20px; }
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='icon'>❌</div>
                    <h1 class='error'>Plaćanje Otkazano!</h1>
                    <p>Niste izvršili plaćanje.</p>
                    <p>Možete zatvoriti ovaj prozor i pokušati ponovo u aplikaciji.</p>
                </div>
            </body>
            </html>";

        return new ContentResult
        {
            Content = html,
            ContentType = "text/html; charset=utf-8"
        };
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
    public string? PaymentMethod { get; set; }
} 