using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Data;
using ŠišAppApi.Models;

namespace ŠišAppApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReportsController(ApplicationDbContext context)
    {
        _context = context;
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
}
