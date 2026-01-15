using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Data;
using ŠišAppApi.Models;

namespace ŠišAppApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SalonsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SalonsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Salons
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetSalons()
    {
        return await _context.Salons
            .Include(s => s.Barbers)
            .Select(s => new 
            {
                s.Id,
                s.Name,
                s.City,
                s.Address,
                s.Phone,
                EmployeeCount = s.Barbers.Count(b => !b.IsDeleted),
                Rating = s.Rating,
                IsActive = s.IsActive
            })
            .ToListAsync();
    }

    // PUT: api/Salons/5/status
    [HttpPut("{id}/status")]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var salon = await _context.Salons.FindAsync(id);
        if (salon == null) return NotFound();

        salon.IsActive = !salon.IsActive;
        await _context.SaveChangesAsync();
        
        return Ok(new { isActive = salon.IsActive });
    }

    // GET: api/Salons/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Salon>> GetSalon(int id)
    {
        var salon = await _context.Salons.FindAsync(id);

        if (salon == null)
        {
            return NotFound();
        }

        return salon;
    }

    // PUT: api/Salons/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutSalon(int id, Salon salon)
    {
        if (id != salon.Id)
        {
            return BadRequest();
        }

        _context.Entry(salon).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SalonExists(id))
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

    // POST: api/Salons
    [HttpPost]
    public async Task<ActionResult<Salon>> PostSalon(Salon salon)
    {
        _context.Salons.Add(salon);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetSalon", new { id = salon.Id }, salon);
    }

    // DELETE: api/Salons/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSalon(int id)
    {
        var salon = await _context.Salons.FindAsync(id);
        if (salon == null)
        {
            return NotFound();
        }

        _context.Salons.Remove(salon);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool SalonExists(int id)
    {
        return _context.Salons.Any(e => e.Id == id);
    }
} 