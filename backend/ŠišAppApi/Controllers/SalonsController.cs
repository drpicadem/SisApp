using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ŠišAppApi.Data;
using ŠišAppApi.Models;
using ŠišAppApi.Services.Interfaces;

using Microsoft.AspNetCore.Authorization;

namespace ŠišAppApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SalonsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IImageService _imageService;

    public SalonsController(ApplicationDbContext context, IImageService imageService)
    {
        _context = context;
        _imageService = imageService;
    }

    // GET: api/Salons
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetSalons()
    {
        return await _context.Salons
            .Include(s => s.Barbers)
            .Include(s => s.Services)
            .Select(s => new 
            {
                s.Id,
                s.Name,
                s.City,
                s.Address,
                s.Phone,
                s.PostalCode,
                s.Country,
                s.Website,
                s.ImageIds,
                s.Latitude,
                s.Longitude,
                Services = s.Services.Where(serv => !serv.IsDeleted && serv.IsActive).Select(serv => serv.Name).ToList(),
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

    // POST: api/Salons/5/upload-image
    [HttpPost("{id}/upload-image")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<Image>> UploadSalonImage(int id, IFormFile file)
    {
        var salon = await _context.Salons.FindAsync(id);
        if (salon == null) return NotFound();

        var image = await _imageService.UploadImageAsync(file, "salon", id, "Salon");

        // Append image ID to ImageIds JSON array
        var imageIds = string.IsNullOrEmpty(salon.ImageIds) 
            ? new List<string>() 
            : JsonSerializer.Deserialize<List<string>>(salon.ImageIds) ?? new List<string>();
        imageIds.Add(image.Id);
        salon.ImageIds = JsonSerializer.Serialize(imageIds);
        await _context.SaveChangesAsync();

        return Ok(image);
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