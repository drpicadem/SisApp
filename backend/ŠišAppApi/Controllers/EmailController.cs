using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Services;
using System;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace ŠišAppApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;

    public EmailController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost("test")]
    public async Task<IActionResult> SendTestEmail([Required][EmailAddress] string toEmail)
    {
        try
        {
            var subject = "Test e-mail - ŠišApp";
            var body = @"
                <h1>Test e-mail</h1>
                <p>Ovo je test e-mail poslan iz ŠišApp aplikacije.</p>
                <p>Vrijeme slanja: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + @"</p>
                <hr>
                <p>ŠišApp Team</p>";

            await _emailService.SendEmailAsync(toEmail, subject, body);
            return Ok(new { message = "Test e-mail uspješno poslan." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Greška prilikom slanja e-maila: {ex.Message}" });
        }
    }

    [HttpPost("appointment-confirmation")]
    public async Task<IActionResult> SendAppointmentConfirmation(
        [Required][EmailAddress] string toEmail,
        [Required] string customerName)
    {
        try
        {
            var subject = "Potvrda termina - ŠišApp";
            var body = @$"
                <h1>Potvrda termina</h1>
                <p>Poštovani {customerName},</p>
                <p>Vaš termin je uspješno rezerviran.</p>
                <p>Detalji termina:</p>
                <ul>
                    <li>Datum: {DateTime.Now.AddDays(1):dd.MM.yyyy}</li>
                    <li>Vrijeme: 10:00</li>
                    <li>Usluga: Muško šišanje</li>
                </ul>
                <p>Hvala Vam na povjerenju!</p>
                <hr>
                <p>ŠišApp Team</p>";

            await _emailService.SendEmailAsync(toEmail, subject, body);
            return Ok(new { message = "E-mail potvrde termina uspješno poslan." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Greška prilikom slanja e-maila: {ex.Message}" });
        }
    }

    [HttpPost("password-reset")]
    public async Task<IActionResult> SendPasswordReset(
        [Required][EmailAddress] string toEmail,
        [Required] string resetToken)
    {
        try
        {
            var subject = "Reset lozinke - ŠišApp";
            var body = @$"
                <h1>Reset lozinke</h1>
                <p>Primili smo zahtjev za resetiranje Vaše lozinke.</p>
                <p>Vaš kod za resetiranje je: <strong>{resetToken}</strong></p>
                <p>Ako niste Vi zatražili resetiranje lozinke, molimo ignorirajte ovaj e-mail.</p>
                <hr>
                <p>ŠišApp Team</p>";

            await _emailService.SendEmailAsync(toEmail, subject, body);
            return Ok(new { message = "E-mail za resetiranje lozinke uspješno poslan." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Greška prilikom slanja e-maila: {ex.Message}" });
        }
    }
} 