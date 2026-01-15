using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Data;
using ŠišAppApi.Models;

namespace ŠišAppApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WorkingHoursController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public WorkingHoursController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/WorkingHours
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkingHours>>> GetWorkingHours()
    {
        return await _context.WorkingHours.ToListAsync();
    }

    // GET: api/WorkingHours/5
    [HttpGet("{id}")]
    public async Task<ActionResult<WorkingHours>> GetWorkingHour(int id)
    {
        var workingHour = await _context.WorkingHours.FindAsync(id);

        if (workingHour == null)
        {
            return NotFound();
        }

        return workingHour;
    }

    // POST: api/WorkingHours
    [HttpPost]
    public async Task<ActionResult<WorkingHours>> PostWorkingHour(WorkingHours workingHour)
    {
        _context.WorkingHours.Add(workingHour);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetWorkingHour", new { id = workingHour.Id }, workingHour);
    }

    // PUT: api/WorkingHours/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutWorkingHour(int id, WorkingHours workingHour)
    {
        if (id != workingHour.Id)
        {
            return BadRequest();
        }

        _context.Entry(workingHour).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!WorkingHourExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // DELETE: api/WorkingHours/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWorkingHour(int id)
    {
        var workingHour = await _context.WorkingHours.FindAsync(id);
        if (workingHour == null)
        {
            return NotFound();
        }

        _context.WorkingHours.Remove(workingHour);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool WorkingHourExists(int id)
    {
        return _context.WorkingHours.Any(e => e.Id == id);
    }
} 