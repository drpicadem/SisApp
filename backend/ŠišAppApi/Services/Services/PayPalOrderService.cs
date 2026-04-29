using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.Json;
using ŠišAppApi.Constants;
using ŠišAppApi.Data;
using ŠišAppApi.Filters;
using ŠišAppApi.Models;
using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Services.Services
{
    public class PayPalOrderService : IPayPalOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentFinalizationService _paymentFinalizationService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<PayPalOrderService> _logger;

        public PayPalOrderService(
            ApplicationDbContext context,
            IPaymentFinalizationService paymentFinalizationService,
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            ILogger<PayPalOrderService> logger)
        {
            _context = context;
            _paymentFinalizationService = paymentFinalizationService;
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
        }

        public async Task<string> CreateOrderAsync(int appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Service)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
                throw new NotFoundException("Appointment not found");

            if (appointment.PaymentStatus == AppointmentPaymentStatuses.Paid ||
                await _context.Payments.AnyAsync(p => p.AppointmentId == appointmentId && p.Status == PaymentStatuses.Completed))
                throw new UserException("Ovaj termin je već plaćen.");

            if (await _context.Payments.AnyAsync(p => p.AppointmentId == appointmentId && p.Status == PaymentStatuses.Pending))
                throw new UserException("Plaćanje je već pokrenuto za ovaj termin.");

            var expectedAmount = decimal.Round(appointment.Service?.Price ?? 0m, 2, MidpointRounding.AwayFromZero);
            if (expectedAmount <= 0m)
                throw new UserException("Nevažeći iznos usluge za plaćanje.");

            var (httpClient, baseUrl, accessToken) = await BuildAuthenticatedClientAsync();
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var paymentBody = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new {
                        amount = new {
                            currency_code = "EUR",
                            value = expectedAmount.ToString("0.00", CultureInfo.InvariantCulture)
                        },
                        description = appointment.Service?.Name ?? "Rezervacija termina",
                        custom_id = appointmentId.ToString()
                    }
                },
                application_context = new { brand_name = "SisApp", user_action = "PAY_NOW" }
            };

            var content = new StringContent(JsonSerializer.Serialize(paymentBody), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"{baseUrl}/v2/checkout/orders", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to create PayPal order. Status: {Status}, Response: {Response}",
                    response.StatusCode, responseBody);
                throw new InvalidOperationException("Failed to create PayPal order");
            }

            var payload = JsonDocument.Parse(responseBody);
            var orderId = payload.RootElement.GetProperty("id").GetString();
            if (string.IsNullOrEmpty(orderId))
                throw new InvalidOperationException("PayPal order ID missing.");

            _context.Payments.Add(new Payment
            {
                AppointmentId = appointmentId,
                UserId = appointment.UserId,
                Amount = expectedAmount,
                Currency = "EUR",
                Method = PaymentMethods.PayPal,
                TransactionId = orderId,
                Status = PaymentStatuses.Pending,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return orderId;
        }

        public async Task<PayPalCaptureResult> CaptureOrderAsync(string orderId, int appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Service)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
                return new PayPalCaptureResult { AppointmentNotFound = true };

            if (appointment.PaymentStatus == AppointmentPaymentStatuses.Paid)
                return new PayPalCaptureResult { AlreadyPaid = true };

            var expectedAmount = decimal.Round(appointment.Service?.Price ?? 0m, 2, MidpointRounding.AwayFromZero);
            if (expectedAmount <= 0m)
                throw new UserException("Nevažeći iznos usluge za plaćanje.");

            var (httpClient, baseUrl, accessToken) = await BuildAuthenticatedClientAsync();
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var orderResponse = await httpClient.GetAsync($"{baseUrl}/v2/checkout/orders/{orderId}");
            var orderBody = await orderResponse.Content.ReadAsStringAsync();
            if (!orderResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to verify PayPal order. Status: {Status}", orderResponse.StatusCode);
                throw new InvalidOperationException("Failed to verify order");
            }

            var orderDoc = JsonDocument.Parse(orderBody);
            if (!TryExtractOrderAmount(orderDoc.RootElement, out var orderAmount, out var orderCurrency))
                throw new UserException("PayPal order data is invalid.");

            if (orderCurrency?.ToUpperInvariant() != "EUR" || orderAmount != expectedAmount)
                throw new UserException("PayPal order amount does not match service catalog amount.");

            var captureContent = new StringContent("{}", Encoding.UTF8, "application/json");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Prefer", "return=representation");
            var captureResponse = await httpClient.PostAsync($"{baseUrl}/v2/checkout/orders/{orderId}/capture", captureContent);
            var captureBody = await captureResponse.Content.ReadAsStringAsync();

            if (!captureResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to capture PayPal order. Status: {Status}", captureResponse.StatusCode);
                throw new InvalidOperationException($"Failed to capture order. PayPal {(int)captureResponse.StatusCode}: {captureBody}");
            }

            var captureDoc = JsonDocument.Parse(captureBody);
            if (!TryExtractCaptureData(captureDoc.RootElement, out var parsedAppointmentId, out var captureId, out var amount, out var currency))
                throw new UserException("PayPal capture response is missing required data.");

            if (parsedAppointmentId.HasValue && appointmentId != parsedAppointmentId.Value)
                throw new UserException("Appointment mismatch between request and PayPal order.");

            var finalizeResult = await _paymentFinalizationService.FinalizeAsync(new PaymentFinalizationInput
            {
                AppointmentId = appointmentId,
                Method = PaymentMethods.PayPal,
                TransactionId = captureId!,
                Amount = amount ?? 0m,
                Currency = currency ?? "EUR"
            });

            if (finalizeResult.AppointmentNotFound)
                return new PayPalCaptureResult { AppointmentNotFound = true };

            return new PayPalCaptureResult
            {
                AlreadyPaid = finalizeResult.AlreadyPaid,
                CaptureId = captureId
            };
        }

        public async Task<PayPalCancelResult> CancelPendingAsync(int appointmentId)
        {
            var pending = await _context.Payments
                .FirstOrDefaultAsync(p =>
                    p.AppointmentId == appointmentId &&
                    p.Status == PaymentStatuses.Pending &&
                    p.Method == PaymentMethods.PayPal);

            if (pending == null)
                return new PayPalCancelResult { HadPending = false };

            pending.Status = PaymentStatuses.Cancelled;
            pending.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new PayPalCancelResult { HadPending = true };
        }

        private async Task<(HttpClient client, string baseUrl, string accessToken)> BuildAuthenticatedClientAsync()
        {
            var httpClient = _httpClientFactory.CreateClient("PayPal");
            var clientId = _config["PayPal:ClientId"] ?? throw new InvalidOperationException("PayPal ClientId is not configured");
            var secret = _config["PayPal:Secret"] ?? throw new InvalidOperationException("PayPal Secret is not configured");
            var baseUrl = _config["PayPal:BaseUrl"] ?? throw new InvalidOperationException("PayPal BaseUrl is not configured");

            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{secret}"));
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);

            var tokenRequest = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");
            var tokenResponse = await httpClient.PostAsync($"{baseUrl}/v1/oauth2/token", tokenRequest);

            if (!tokenResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get PayPal access token. Status: {Status}", tokenResponse.StatusCode);
                throw new InvalidOperationException("Failed to authenticate with PayPal");
            }

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var accessToken = JsonDocument.Parse(tokenJson).RootElement.GetProperty("access_token").GetString()
                ?? throw new InvalidOperationException("PayPal access token missing");

            return (httpClient, baseUrl, accessToken);
        }

        private static bool TryExtractOrderAmount(JsonElement root, out decimal? amount, out string? currency)
        {
            amount = null; currency = null;
            if (!root.TryGetProperty("purchase_units", out var units) ||
                units.ValueKind != JsonValueKind.Array || units.GetArrayLength() == 0)
                return false;

            var unit = units[0];
            if (!unit.TryGetProperty("amount", out var amountEl)) return false;
            currency = amountEl.TryGetProperty("currency_code", out var c) ? c.GetString() : null;
            if (amountEl.TryGetProperty("value", out var v) &&
                decimal.TryParse(v.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                amount = parsed;
            return amount.HasValue && !string.IsNullOrWhiteSpace(currency);
        }

        private static bool TryExtractCaptureData(JsonElement root, out int? appointmentId, out string? captureId, out decimal? amount, out string? currency)
        {
            appointmentId = null; captureId = null; amount = null; currency = null;
            if (!root.TryGetProperty("purchase_units", out var units) ||
                units.ValueKind != JsonValueKind.Array || units.GetArrayLength() == 0)
                return false;

            var unit = units[0];
            if (unit.TryGetProperty("custom_id", out var customId) &&
                int.TryParse(customId.GetString(), out var parsedId))
                appointmentId = parsedId;

            if (unit.TryGetProperty("payments", out var payments) &&
                payments.TryGetProperty("captures", out var captures) &&
                captures.ValueKind == JsonValueKind.Array && captures.GetArrayLength() > 0)
            {
                var capture = captures[0];
                captureId = capture.TryGetProperty("id", out var cid) ? cid.GetString() : null;
                if (capture.TryGetProperty("amount", out var amEl))
                {
                    currency = amEl.TryGetProperty("currency_code", out var cc) ? cc.GetString() : null;
                    if (amEl.TryGetProperty("value", out var v) &&
                        decimal.TryParse(v.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                        amount = parsed;
                }
            }
            return !string.IsNullOrEmpty(captureId) && amount.HasValue;
        }
    }
}
