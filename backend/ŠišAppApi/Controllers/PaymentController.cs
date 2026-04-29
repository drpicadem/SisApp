using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using ŠišAppApi.Services;
using Microsoft.AspNetCore.Authorization;

namespace ŠišAppApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _configuration;

    public PaymentController(
        IPaymentService paymentService,
        IConfiguration configuration)
    {
        _paymentService = paymentService;
        _configuration = configuration;
    }

    [HttpPost("cancel-pending")]
    public async Task<IActionResult> CancelPending([FromBody] CancelPendingStripeRequest request)
    {
        return await _paymentService.CancelPending(request);
    }

    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var endpointSecret = _configuration["Stripe:WebhookSecret"];
        var stripeSignature = Request.Headers["Stripe-Signature"].ToString();
        return await _paymentService.HandleWebhook(json, stripeSignature, endpointSecret);
    }
}

public class CancelPendingStripeRequest
{
    [Required]
    public int? AppointmentId { get; set; }
}
