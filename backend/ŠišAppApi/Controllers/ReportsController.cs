using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Data;
using ŠišAppApi.Models;

using Microsoft.AspNetCore.Authorization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ŠišAppApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReportsController(ApplicationDbContext context)
    {
        _context = context;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // GET: api/Reports/stats
    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetStats()
    {
        var totalUsers = await _context.Users.CountAsync(u => u.Role == "User" && u.IsActive);
        var totalBarbers = await _context.Barbers.CountAsync();
        var totalSalons = await _context.Salons.CountAsync();
        
        // For line chart: Users created per month in 2024/2025
        var currentYear = DateTime.Now.Year;
        var monthlyRegistrations = await _context.Users
            .Where(u => u.Role == "User" && u.CreatedAt.Year == currentYear)
            .GroupBy(u => u.CreatedAt.Month)
            .Select(g => new { Month = g.Key, Count = g.Count() })
            .OrderBy(x => x.Month)
            .ToListAsync();

        return Ok(new
        {
            TotalUsers = totalUsers,
            TotalBarbers = totalBarbers,
            TotalSalons = totalSalons,
            MonthlyRegistrations = monthlyRegistrations
        });
    }

    // GET: api/Reports/stats/pdf
    [HttpGet("stats/pdf")]
    public async Task<IActionResult> GetStatsPdf()
    {
        var totalUsers = await _context.Users.CountAsync(u => u.Role == "User" && u.IsActive);
        var totalBarbers = await _context.Barbers.CountAsync();
        var totalSalons = await _context.Salons.CountAsync();
        
        var currentYear = DateTime.Now.Year;
        var monthlyRegistrations = await _context.Users
            .Where(u => u.Role == "User" && u.CreatedAt.Year == currentYear)
            .GroupBy(u => u.CreatedAt.Month)
            .Select(g => new { Month = g.Key, Count = g.Count() })
            .OrderBy(x => x.Month)
            .ToListAsync();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Element(ComposeHeader);
                page.Content().Element(x => ComposeContent(x, totalUsers, totalBarbers, totalSalons, monthlyRegistrations.Cast<object>().ToList()));
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        });

        byte[] pdfBytes = document.GeneratePdf();
        return File(pdfBytes, "application/pdf", "Statistika.pdf");
    }

    void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("Izvještaj: Statistika ŠišApp").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Text(text =>
                {
                    text.Span("Datum izrade: ").SemiBold();
                    text.Span($"{DateTime.Now:dd.MM.yyyy}");
                });
            });
        });
    }

    void ComposeContent(IContainer container, int users, int barbers, int salons, List<object> monthlyData)
    {
        container.PaddingVertical(1, Unit.Centimetre).Column(column =>
        {
            column.Spacing(20);

            column.Item().Text("Opšti pregled").FontSize(16).SemiBold();
            
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().BorderBottom(1).Padding(5).Text("Korisnici").SemiBold();
                    header.Cell().BorderBottom(1).Padding(5).Text("Frizeri").SemiBold();
                    header.Cell().BorderBottom(1).Padding(5).Text("Saloni").SemiBold();
                });

                table.Cell().Padding(5).Text(users.ToString());
                table.Cell().Padding(5).Text(barbers.ToString());
                table.Cell().Padding(5).Text(salons.ToString());
            });

            column.Item().Text("Mjesečne registracije korisnika").FontSize(16).SemiBold();

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().BorderBottom(1).Padding(5).Text("Mjesec").SemiBold();
                    header.Cell().BorderBottom(1).Padding(5).Text("Broj novih korisnika (za trenutnu godinu)").SemiBold();
                });

                foreach (var item in monthlyData)
                {
                    var month = item.GetType().GetProperty("Month")!.GetValue(item)!.ToString()!;
                    var count = item.GetType().GetProperty("Count")!.GetValue(item)!.ToString()!;
                    table.Cell().Padding(5).Text(month);
                    table.Cell().Padding(5).Text(count);
                }
            });
        });
    }
}
