using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Controllers;

using Microsoft.AspNetCore.Authorization;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PayPalController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ILogger<PayPalController> _logger;
    private readonly IPayPalOrderService _payPalOrderService;

    public PayPalController(
        IConfiguration config,
        ILogger<PayPalController> logger,
        IPayPalOrderService payPalOrderService)
    {
        _config = config;
        _logger = logger;
        _payPalOrderService = payPalOrderService;
    }

    [HttpGet("mobile-config")]
    public IActionResult GetMobileConfig()
    {
        var clientId = _config["PayPal:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
            return StatusCode(500, new { error = "PayPal ClientId is not configured" });

        var environment = _config["PayPal:Environment"] ?? "sandbox";
        return Ok(new { clientId, environment = environment.ToLowerInvariant() });
    }

    [HttpPost("create-order")]
    public async Task<IActionResult> CreateOrder([FromBody] PayPalCreateOrderRequest request)
    {
        try
        {
            if (!request.AppointmentId.HasValue)
                return BadRequest("AppointmentId je obavezan.");

            var orderId = await _payPalOrderService.CreateOrderAsync(request.AppointmentId.Value);
            return Ok(new { orderId });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to authenticate with PayPal", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(ex, "PayPal authentication failed while creating order");
            return StatusCode(500, new { error = "PayPal sandbox kredencijali nisu validni. Provjerite PayPal ClientId i Secret u .env." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PayPal order");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("capture-order")]
    public async Task<IActionResult> CaptureOrder([FromBody] CaptureOrderRequest request)
    {
        try
        {
            if (!request.AppointmentId.HasValue)
                return BadRequest("AppointmentId je obavezan.");

            var result = await _payPalOrderService.CaptureOrderAsync(request.OrderId, request.AppointmentId.Value);

            if (result.AppointmentNotFound)
                return NotFound(new { error = "Appointment not found" });

            if (result.AlreadyPaid)
                return Ok(new { status = "Paid", alreadyPaid = true });

            return Ok(new { status = "Paid", alreadyPaid = false, transactionId = result.CaptureId });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to authenticate with PayPal", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(ex, "PayPal authentication failed while capturing order");
            return StatusCode(500, new { error = "PayPal sandbox kredencijali nisu validni. Provjerite PayPal ClientId i Secret u .env." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing PayPal order");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("cancel-pending")]
    public async Task<IActionResult> CancelPending([FromBody] CancelPendingOrderRequest request)
    {
        if (!request.AppointmentId.HasValue)
            return BadRequest("AppointmentId je obavezan.");

        var result = await _payPalOrderService.CancelPendingAsync(request.AppointmentId.Value);
        return Ok(new { status = result.HadPending ? "Cancelled" : "NoPending" });
    }
}

public class PayPalCreateOrderRequest
{
    [Required]
    public int? AppointmentId { get; set; }
}

public class CaptureOrderRequest
{
    [Required]
    public string OrderId { get; set; } = string.Empty;

    [Required]
    public int? AppointmentId { get; set; }
}

public class CancelPendingOrderRequest
{
    [Required]
    public int? AppointmentId { get; set; }
}
