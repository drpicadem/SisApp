using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace ŠišAppApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PayPalController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<PayPalController> _logger;

    public PayPalController(IConfiguration config, ILogger<PayPalController> logger)
    {
        _config = config;
        _httpClient = new HttpClient();
        _logger = logger;
    }

    [HttpPost("create-payment")]
    public async Task<IActionResult> CreatePayment([FromBody] PayPalPaymentRequest request)
    {
        try
        {
            var clientId = _config["PayPal:ClientId"] ?? 
                throw new InvalidOperationException("PayPal ClientId is not configured");
            var secret = _config["PayPal:Secret"] ?? 
                throw new InvalidOperationException("PayPal Secret is not configured");
            var baseUrl = _config["PayPal:BaseUrl"] ?? 
                throw new InvalidOperationException("PayPal BaseUrl is not configured");

            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{secret}"));

            // 1. Get access token
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);
            var tokenRequest = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");
            var tokenResponse = await _httpClient.PostAsync($"{baseUrl}/v1/oauth2/token", tokenRequest);
            
            if (!tokenResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get PayPal access token. Status: {Status}, Response: {Response}", 
                    tokenResponse.StatusCode, await tokenResponse.Content.ReadAsStringAsync());
                return StatusCode(500, "Failed to authenticate with PayPal");
            }

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var accessToken = JsonDocument.Parse(tokenJson).RootElement.GetProperty("access_token").GetString();

            // 2. Create payment
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var paymentBody = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new {
                        amount = new {
                            currency_code = request.Currency,
                            value = request.Amount.ToString("0.00")
                        },
                        description = request.ServiceDescription,
                        custom_id = request.AppointmentId?.ToString()
                    }
                },
                application_context = new
                {
                    return_url = request.ReturnUrl,
                    cancel_url = request.CancelUrl,
                    brand_name = "ŠišApp",
                    locale = "hr-HR",
                    landing_page = "LOGIN",
                    user_action = "PAY_NOW"
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(paymentBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{baseUrl}/v2/checkout/orders", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to create PayPal order. Status: {Status}, Response: {Response}", 
                    response.StatusCode, responseBody);
                return StatusCode(500, "Failed to create PayPal order");
            }

            return Content(responseBody, "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PayPal payment");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("capture-payment")]
    public async Task<IActionResult> CapturePayment([FromBody] CapturePaymentRequest request)
    {
        try
        {
            var clientId = _config["PayPal:ClientId"] ?? 
                throw new InvalidOperationException("PayPal ClientId is not configured");
            var secret = _config["PayPal:Secret"] ?? 
                throw new InvalidOperationException("PayPal Secret is not configured");
            var baseUrl = _config["PayPal:BaseUrl"] ?? 
                throw new InvalidOperationException("PayPal BaseUrl is not configured");

            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{secret}"));

            // 1. Get access token
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);
            var tokenRequest = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");
            var tokenResponse = await _httpClient.PostAsync($"{baseUrl}/v1/oauth2/token", tokenRequest);
            
            if (!tokenResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get PayPal access token. Status: {Status}, Response: {Response}", 
                    tokenResponse.StatusCode, await tokenResponse.Content.ReadAsStringAsync());
                return StatusCode(500, "Failed to authenticate with PayPal");
            }

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var accessToken = JsonDocument.Parse(tokenJson).RootElement.GetProperty("access_token").GetString();

            // 2. Capture payment
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _httpClient.PostAsync($"{baseUrl}/v2/checkout/orders/{request.OrderId}/capture", null);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to capture PayPal payment. Status: {Status}, Response: {Response}", 
                    response.StatusCode, responseBody);
                return StatusCode(500, "Failed to capture payment");
            }

            return Content(responseBody, "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing PayPal payment");
            return StatusCode(500, "Internal server error");
        }
    }
}

public class PayPalPaymentRequest
{
    [Required]
    public decimal Amount { get; set; }

    [Required]
    public string Currency { get; set; } = "EUR";

    [Required]
    public string ServiceDescription { get; set; } = string.Empty;

    [Required]
    public string ReturnUrl { get; set; } = string.Empty;

    [Required]
    public string CancelUrl { get; set; } = string.Empty;

    public int? AppointmentId { get; set; }
}

public class CapturePaymentRequest
{
    [Required]
    public string OrderId { get; set; } = string.Empty;
} 