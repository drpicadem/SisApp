using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Services;

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
        if (!request.AppointmentId.HasValue)
            return BadRequest("AppointmentId je obavezan.");

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        return await _paymentService.CancelPending(request.AppointmentId.Value, userId);
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
