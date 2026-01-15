using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Services;
using System;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace ŠišAppApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SmsController : ControllerBase
{
    private readonly ISmsService _smsService;

    public SmsController(ISmsService smsService)
    {
        _smsService = smsService;
    }

    [HttpPost("test")]
    public async Task<IActionResult> SendTestSms([Required] string toPhoneNumber)
    {
        try
        {
            var message = $"Test SMS iz ŠišApp aplikacije. Vrijeme slanja: {DateTime.Now:dd.MM.yyyy HH:mm:ss}";
            await _smsService.SendSmsAsync(toPhoneNumber, message);
            return Ok(new { message = "Test SMS uspješno poslan." });
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
            return StatusCode(500, new { error = $"Greška prilikom slanja SMS-a: {ex.Message}" });
        }
    }

    [HttpPost("appointment-reminder")]
    public async Task<IActionResult> SendAppointmentReminder(
        [Required] string toPhoneNumber,
        [Required] string customerName)
    {
        try
        {
            var message = $"Poštovani {customerName},\n" +
                         $"Podsjećamo Vas na Vaš termin sutra u 10:00.\n" +
                         $"Hvala Vam na povjerenju!\n" +
                         $"ŠišApp Team";

            await _smsService.SendSmsAsync(toPhoneNumber, message);
            return Ok(new { message = "SMS podsjetnika uspješno poslan." });
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
            return StatusCode(500, new { error = $"Greška prilikom slanja SMS-a: {ex.Message}" });
        }
    }
} 