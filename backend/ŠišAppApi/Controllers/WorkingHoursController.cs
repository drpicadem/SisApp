using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Data;
using ŠišAppApi.Models;
using ŠišAppApi.Filters;
using System.Security.Claims;

namespace ŠišAppApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WorkingHoursController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public WorkingHoursController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim))
            throw new UserException("Korisnik nije autentificiran.");
        return int.Parse(claim);
    }

    private async Task<Barber> GetCurrentBarber()
    {
        var userId = GetUserId();
        var barber = await _context.Barbers.FirstOrDefaultAsync(b => b.UserId == userId);
        if (barber == null)
            throw new UserException("Niste registrirani kao frizer.");
        return barber;
    }

    /// <summary>
    /// Get working hours for the currently logged-in barber
    /// </summary>
    [HttpGet("my-schedule")]
    public async Task<ActionResult<List<WorkingHours>>> GetMySchedule()
    {
        var barber = await GetCurrentBarber();

        var schedule = await _context.WorkingHours
            .Where(wh => wh.BarberId == barber.Id && !wh.IsDeleted)
            .OrderBy(wh => wh.DayOfWeek)
            .ThenBy(wh => wh.StartTime)
            .ToListAsync();

        return Ok(schedule);
    }

    /// <summary>
    /// Get working hours for a specific barber (public)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("barber/{barberId}")]
    public async Task<ActionResult<List<WorkingHours>>> GetBarberSchedule(int barberId)
    {
        var schedule = await _context.WorkingHours
            .Where(wh => wh.BarberId == barberId && !wh.IsDeleted && wh.IsWorking)
            .OrderBy(wh => wh.DayOfWeek)
            .ThenBy(wh => wh.StartTime)
            .ToListAsync();

        return Ok(schedule);
    }

    /// <summary>
    /// Create working hours for the logged-in barber
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<WorkingHours>> CreateWorkingHours([FromBody] WorkingHoursCreateDto dto)
    {
        var barber = await GetCurrentBarber();

        // Check for overlapping hours on same day
        var existing = await _context.WorkingHours
            .FirstOrDefaultAsync(wh => wh.BarberId == barber.Id
                && wh.DayOfWeek == dto.DayOfWeek
                && !wh.IsDeleted);

        if (existing != null)
            throw new UserException("Već imate definirano radno vrijeme za taj dan. Uredite postojeće.");

        var workingHours = new WorkingHours
        {
            BarberId = barber.Id,
            DayOfWeek = dto.DayOfWeek,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            IsWorking = dto.IsWorking,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.WorkingHours.Add(workingHours);
        await _context.SaveChangesAsync();

        return Ok(workingHours);
    }

    /// <summary>
    /// Update working hours (barber can only update own)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<WorkingHours>> UpdateWorkingHours(int id, [FromBody] WorkingHoursCreateDto dto)
    {
        var barber = await GetCurrentBarber();

        var workingHours = await _context.WorkingHours.FindAsync(id);
        if (workingHours == null)
            throw new UserException("Radno vrijeme nije pronađeno.");

        if (workingHours.BarberId != barber.Id)
            throw new UserException("Možete ažurirati samo svoje radno vrijeme.");

        workingHours.DayOfWeek = dto.DayOfWeek;
        workingHours.StartTime = dto.StartTime;
        workingHours.EndTime = dto.EndTime;
        workingHours.IsWorking = dto.IsWorking;
        workingHours.Notes = dto.Notes;
        workingHours.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(workingHours);
    }

    /// <summary>
    /// Delete (soft) working hours (barber can only delete own)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWorkingHours(int id)
    {
        var barber = await GetCurrentBarber();

        var workingHours = await _context.WorkingHours.FindAsync(id);
        if (workingHours == null)
            throw new UserException("Radno vrijeme nije pronađeno.");

        if (workingHours.BarberId != barber.Id)
            throw new UserException("Možete obrisati samo svoje radno vrijeme.");

        workingHours.IsDeleted = true;
        workingHours.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Radno vrijeme obrisano." });
    }
}

public class WorkingHoursCreateDto
{
    public int DayOfWeek { get; set; } // 0 = Sunday, 6 = Saturday
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsWorking { get; set; } = true;
    public string? Notes { get; set; }
}