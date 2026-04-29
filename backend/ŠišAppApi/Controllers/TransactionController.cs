using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Stripe;
using ŠišAppApi.Constants;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TransactionController> _logger;
    private readonly ICurrentUserService _currentUser;
    private readonly IWebHostEnvironment _env;
    private readonly IStripeTransactionService _stripeService;
    private readonly string _stripeSecret;
    private readonly string _stripePublishableKey;
    private readonly bool _isSecretFromEnv;
    private readonly bool _isPublishableFromEnv;

    public TransactionController(
        IConfiguration configuration,
        ILogger<TransactionController> logger,
        ICurrentUserService currentUser,
        IWebHostEnvironment env,
        IStripeTransactionService stripeService)
    {
        _configuration = configuration;
        _logger = logger;
        _currentUser = currentUser;
        _env = env;
        _stripeService = stripeService;

        var envSecret = Environment.GetEnvironmentVariable("stripe");
        var cfgSecret = _configuration["Stripe:SecretKey"];
        _isSecretFromEnv = !string.IsNullOrWhiteSpace(envSecret);
        _stripeSecret = _isSecretFromEnv ? envSecret! : (cfgSecret ?? string.Empty);

        var envPublishableKey = Environment.GetEnvironmentVariable("_stripePublishableKey");
        var cfgPublishableKey = _configuration["Stripe:PublishableKey"];
        _isPublishableFromEnv = !string.IsNullOrWhiteSpace(envPublishableKey);
        _stripePublishableKey = _isPublishableFromEnv ? envPublishableKey! : (cfgPublishableKey ?? string.Empty);
    }

    [HttpGet("payment-form")]
    public async Task<IActionResult> PaymentForm(
        [FromQuery] int appointmentId,
        [FromQuery] string? token = null,
        [FromQuery(Name = "authToken")] string? authToken = null,
        [FromQuery] string? clientPlatform = null)
    {
        token = ResolveIncomingToken(token, authToken);
        if (string.IsNullOrWhiteSpace(token))
            return Content(BuildErrorHtml("Nedostaje token za autorizaciju."), "text/html; charset=utf-8");

        var principal = ValidateTokenFromQuery(token, out var tokenError);
        if (principal == null)
            return Content(BuildErrorHtml($"Nevažeći token: {tokenError}"), "text/html; charset=utf-8");

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Content(BuildErrorHtml("Token ne sadrži validan korisnički ID."), "text/html; charset=utf-8");

        var appointmentData = await _stripeService.GetAppointmentForPaymentAsync(appointmentId, userId);

        if (appointmentData == null)
            return Content(BuildErrorHtml("Termin nije pronađen."), "text/html; charset=utf-8");

        if (appointmentData.Id == -1)
            return Content(BuildErrorHtml("Nemate pravo pristupa ovom plaćanju."), "text/html; charset=utf-8");

        if (appointmentData.AlreadyPaid)
            return Content(BuildDoneHtml("Plaćanje je već završeno.", appointmentId, "success"), "text/html; charset=utf-8");

        var publishableKey = _stripePublishableKey;
        if (string.IsNullOrWhiteSpace(publishableKey))
            return Content(BuildErrorHtml("Stripe publishable key nije konfigurisan."), "text/html; charset=utf-8");

        if (appointmentData.AmountInCents <= 0)
            return Content(BuildErrorHtml("Iznos za plaćanje mora biti veći od 0."), "text/html; charset=utf-8");

        try
        {
            var intentData = await _stripeService.CreatePaymentIntentAsync(appointmentId, appointmentData.AmountInCents);

            return Content(BuildPaymentHtml(
                publishableKey,
                intentData.ClientSecret,
                appointmentId,
                token,
                intentData.AmountInCents,
                string.Equals(clientPlatform, "mobile", StringComparison.OrdinalIgnoreCase)), "text/html; charset=utf-8");
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error while preparing embedded payment form");
            return Content(BuildErrorHtml("Stripe greška. Pokušajte ponovo."), "text/html; charset=utf-8");
        }
    }

    [Authorize]
    [HttpPost("complete-purchase")]
    public async Task<IActionResult> CompletePurchase([FromBody] CompletePurchaseRequest request)
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized(new { error = "Nevažeći korisnik." });

        try
        {
            var result = await _stripeService.CompletePurchaseAsync(
                request.AppointmentId, _currentUser.UserId.Value, request.PaymentIntentId);

            if (result.AlreadyCompleted)
                return Ok(new { status = AppointmentPaymentStatuses.Paid, alreadyCompleted = true });

            if (result.PaymentNotSucceeded)
                return BadRequest(new { error = $"Plaćanje nije uspješno (status: {result.StripeStatus}).", stripeStatus = result.StripeStatus });

            if (result.AmountMismatch)
                return BadRequest(new { error = "Neispravan iznos plaćanja." });

            return Ok(new { status = AppointmentPaymentStatuses.Paid, alreadyCompleted = false });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error while completing purchase");
            return BadRequest(new { error = "Plaćanje nije uspjelo. Pokušajte ponovo." });
        }
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet("stripe-debug")]
    public async Task<IActionResult> StripeDebug()
    {
        if (!_env.IsDevelopment())
            return NotFound();

        var publishable = _stripePublishableKey;
        string? stripeAccountId = null;
        bool accountFetchOk = false;

        try
        {
            var balance = await new BalanceService().GetAsync();
            stripeAccountId = balance.Object;
            accountFetchOk = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stripe account debug fetch failed");
        }

        return Ok(new
        {
            secretSource = _isSecretFromEnv ? "env:stripe" : "appsettings:Stripe:SecretKey",
            publishableSource = _isPublishableFromEnv ? "env:_stripePublishableKey" : "appsettings:Stripe:PublishableKey",
            hasSecret = !string.IsNullOrWhiteSpace(_stripeSecret),
            hasPublishable = !string.IsNullOrWhiteSpace(publishable),
            secretPrefix = MaskPrefix(_stripeSecret),
            publishablePrefix = MaskPrefix(publishable),
            stripeAccountId,
            accountFetchOk
        });
    }

    private ClaimsPrincipal? ValidateTokenFromQuery(string token, out string? error)
    {
        error = null;
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey)) { error = "JWT key nije konfigurisan."; return null; }

            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            }, out _);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            error = "Token validation failed.";
            return null;
        }
    }

    private static string MaskPrefix(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var take = Math.Min(10, value.Length);
        return $"{value[..take]}...";
    }

    private static string JsLiteral(string value) => JsonSerializer.Serialize(value);

    private string? ResolveIncomingToken(string? queryToken, string? authToken)
    {
        var token = !string.IsNullOrWhiteSpace(queryToken) ? queryToken : authToken;
        if (!string.IsNullOrWhiteSpace(token)) return token;

        var authorization = Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization) &&
            authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authorization["Bearer ".Length..].Trim();

        return null;
    }

    private static string BuildErrorHtml(string message)
    {
        return $$$"""
<!DOCTYPE html>
<html>
<head>
  <meta charset="UTF-8" />
  <title>Plaćanje - Greška</title>
  <style>
    body { font-family: Arial, sans-serif; background:#f5f9ff; margin:0; padding:20px; }
    .card { max-width:680px; margin:0 auto; background:#fff; border:1px solid #d7e6ff; border-radius:12px; padding:18px; }
    .err { color:#b42318; background:#ffe4e8; border:1px solid #ffcdd8; padding:10px; border-radius:8px; }
  </style>
</head>
<body>
  <div class="card">
    <h3 style="color:#175cd3;">Stripe plaćanje</h3>
    <div class="err">{{{System.Net.WebUtility.HtmlEncode(message)}}}</div>
  </div>
</body>
</html>
""";
    }

    private static string BuildDoneHtml(string message, int appointmentId, string status)
    {
        var json = JsonSerializer.Serialize(new { type = "sisapp-embedded-payment", status, appointmentId });
        return $$$"""
<!DOCTYPE html>
<html>
<head><meta charset="UTF-8" /></head>
<body style="font-family:Arial,sans-serif;background:#f5f9ff;">
  <div style="max-width:680px;margin:20px auto;background:#fff;border:1px solid #d7e6ff;border-radius:12px;padding:18px;">
    <h3 style="color:#175cd3;">Stripe plaćanje</h3>
    <p>{{{System.Net.WebUtility.HtmlEncode(message)}}}</p>
  </div>
  <script>
    window.parent.postMessage({{{JsLiteral(json)}}}, '*');
  </script>
</body>
</html>
""";
    }

    private static string BuildPaymentHtml(string publishableKey, string clientSecret, int appointmentId, string token, long amountInCents, bool mobileClient)
    {
        var messagePayloadSuccess = JsonSerializer.Serialize(new { type = "sisapp-embedded-payment", status = "success", appointmentId });
        var messagePayloadCancel = JsonSerializer.Serialize(new { type = "sisapp-embedded-payment", status = "cancel", appointmentId });

        return $$$"""
<!DOCTYPE html>
<html>
<head>
  <meta charset="UTF-8" />
  <title>Plaćanje karticom</title>
  <script src="https://js.stripe.com/v3/"></script>
  <style>
    body { font-family: Arial, sans-serif; background: #f5f9ff; margin: 0; padding: 16px; }
    .card { max-width: 680px; margin: 0 auto; background: #fff; border-radius: 12px; border: 1px solid #d7e6ff; box-shadow: 0 4px 16px rgba(23,92,211,0.08); }
    .head { padding: 14px 16px; border-bottom: 1px solid #e4ecff; color: #175cd3; font-weight: 700; }
    .body { padding: 16px; }
    .hint { color: #175cd3; margin-bottom: 12px; }
    .btn-row { display: flex; gap: 8px; margin-top: 14px; }
    button { border: 0; border-radius: 8px; padding: 10px 14px; cursor: pointer; }
    .pay { background: #175cd3; color: #fff; }
    .cancel { background: #e4ecff; color: #175cd3; }
    .msg { margin-top: 10px; color: #b42318; min-height: 22px; }
  </style>
</head>
<body>
  <div class="card">
    <div class="head">Plaćanje karticom (Stripe)</div>
    <div class="body">
      <div class="hint">Iznos: {{{(amountInCents / 100.0m):0.00}}} EUR</div>
      <form id="payment-form">
        <div id="payment-element"></div>
        <div class="btn-row">
          <button class="pay" id="submit-btn" type="submit">Plati</button>
          <button class="cancel" id="cancel-btn" type="button">Odustani</button>
        </div>
        <div class="msg" id="error-message"></div>
      </form>
    </div>
  </div>

  <script>
    const pk = {{{JsLiteral(publishableKey)}}};
    const clientSecret = {{{JsLiteral(clientSecret)}}};
    const token = {{{JsLiteral(token)}}};
    const appointmentId = {{{appointmentId}}};
    const mobileClient = {{{(mobileClient ? "true" : "false")}}};
    const successPayload = {{{JsLiteral(messagePayloadSuccess)}}};
    const cancelPayload = {{{JsLiteral(messagePayloadCancel)}}};
    const mobileSuccessUrl = 'sisapp-payment://result?status=success&appointmentId=' + appointmentId;
    const mobileCancelUrl = 'sisapp-payment://result?status=cancel&appointmentId=' + appointmentId;

    const stripe = Stripe(pk);
    const elements = stripe.elements({ clientSecret, appearance: { theme: 'stripe', variables: { colorPrimary: '#175cd3' }} });
    const paymentElement = elements.create('payment', { paymentMethodOrder: ['card'] });
    paymentElement.mount('#payment-element');

    const form = document.getElementById('payment-form');
    const errorMessage = document.getElementById('error-message');
    const cancelBtn = document.getElementById('cancel-btn');

    cancelBtn.addEventListener('click', () => {
      if (mobileClient) { window.location.href = mobileCancelUrl; }
      else { window.parent.postMessage(cancelPayload, '*'); }
    });

    form.addEventListener('submit', async (e) => {
      e.preventDefault();
      errorMessage.textContent = '';

      const result = await stripe.confirmPayment({ elements, redirect: 'if_required' });

      if (result.error) { errorMessage.textContent = result.error.message || 'Plaćanje nije uspjelo.'; return; }
      if (!result.paymentIntent || result.paymentIntent.status !== 'succeeded') {
        errorMessage.textContent = 'Plaćanje nije završeno. Status: ' + (result.paymentIntent ? result.paymentIntent.status : 'unknown');
        return;
      }

      const completeRes = await fetch('/api/Transaction/complete-purchase', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + token },
        body: JSON.stringify({ appointmentId: appointmentId, paymentIntentId: result.paymentIntent.id })
      });

      let completeBody = {};
      try { completeBody = await completeRes.json(); } catch (e) {}

      if (!completeRes.ok) {
        const err = completeBody.error || completeBody.message || 'Greška pri potvrdi kupovine.';
        errorMessage.textContent = err.toLowerCase().includes('client_secret') || err.toLowerCase().includes('account')
          ? 'Key mismatch (pk/sk nisu iz istog Stripe accounta).'
          : err;
        return;
      }

      if (mobileClient) { window.location.href = mobileSuccessUrl; }
      else { window.parent.postMessage(successPayload, '*'); }
    });
  </script>
</body>
</html>
""";
    }
}

public class CompletePurchaseRequest
{
    public int AppointmentId { get; set; }
    public string? PaymentIntentId { get; set; }
}
