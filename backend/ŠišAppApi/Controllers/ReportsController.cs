using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Constants;
using ŠišAppApi.Services.Interfaces;

using Microsoft.AspNetCore.Authorization;

namespace ŠišAppApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = AppRoles.Admin)]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _reportService.GetStatsAsync();
        return Ok(stats);
    }

    [HttpGet("stats/pdf")]
    public async Task<IActionResult> GetStatsPdf()
    {
        var pdfBytes = await _reportService.GenerateStatsPdfAsync();
        return File(pdfBytes, "application/pdf", "Statistika.pdf");
    }

    [HttpGet("appointments/pdf")]
    public async Task<IActionResult> GetAppointmentsPdf()
    {
        var pdfBytes = await _reportService.GenerateAppointmentsPdfAsync();
        return File(pdfBytes, "application/pdf", "Rezervacije.pdf");
    }

    [HttpGet("revenue/pdf")]
    public async Task<IActionResult> GetRevenuePdf()
    {
        var pdfBytes = await _reportService.GenerateRevenuePdfAsync();
        return File(pdfBytes, "application/pdf", "Prihodi.pdf");
    }
}
