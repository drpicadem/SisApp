using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ŠišAppApi.Data;
using ŠišAppApi.Models;
using ŠišAppApi.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ŠišAppApi.Filters;

namespace ŠišAppApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BarbersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IImageService _imageService;

    public BarbersController(ApplicationDbContext context, IImageService imageService)
    {
        _context = context;
        _imageService = imageService;
    }

    [HttpGet("my-profile")]
    public async Task<ActionResult<object>> GetMyProfile()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId))
        {
            return Unauthorized();
        }

        var barber = await _context.Barbers
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.UserId == userId && !b.IsDeleted);

        if (barber == null)
            return NotFound("Niste prijavljeni kao aktivan frizer.");

        return Ok(new {
            barber.Id,
            barber.UserId,
            barber.SalonId,
            barber.Rating,
            barber.Bio,
            barber.ImageIds,
            FirstName = barber.User.FirstName,
            LastName = barber.User.LastName,
            Email = barber.User.Email,
            Username = barber.User.Username
        });
    }

    // GET: api/Barbers/salon/1
    [HttpGet("salon/{salonId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetBarbersBySalon(int salonId)
    {
        // Returning anonymous object to flatten User details
        var barbers = await _context.Barbers
            .Include(b => b.User)
            .Where(b => b.SalonId == salonId && !b.IsDeleted)
            .Select(b => new {
                b.Id,
                b.UserId,
                b.SalonId,
                b.Rating,
                b.Bio,
                b.ImageIds,
                FirstName = b.User.FirstName,
                LastName = b.User.LastName,
                Email = b.User.Email,
                Username = b.User.Username
            })
            .ToListAsync();

        return Ok(barbers);
    }

    // POST: api/Barbers
    [HttpPost]
    public async Task<ActionResult<Barber>> PostBarber(CreateBarberDto dto)
    {
        // 1. Check if user email exists
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email || u.Username == dto.Username))
        {
            return BadRequest(new { message = "Korisničko ime ili email već postoje." });
        }

        // 2. Create User
        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = "Barber",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            IsEmailVerified = true // Auto verify for admin created stuff
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // 3. Create Barber Profile
        var barber = new Barber
        {
            UserId = user.Id,
            SalonId = dto.SalonId,
            Bio = dto.Bio,
            CreatedAt = DateTime.UtcNow,
            Rating = 5.0 // Default start rating
        };

        _context.Barbers.Add(barber);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetBarbersBySalon", new { salonId = dto.SalonId }, barber);
    }

    // POST: api/Barbers/5/upload-image
    [HttpPost("{id}/upload-image")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<Image>> UploadBarberImage(int id, IFormFile file)
    {
        var barber = await _context.Barbers.FindAsync(id);
        if (barber == null) return NotFound();

        var image = await _imageService.UploadImageAsync(file, "barber", id, "Barber");

        var imageIds = string.IsNullOrEmpty(barber.ImageIds)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(barber.ImageIds) ?? new List<string>();
        imageIds.Add(image.Id);
        barber.ImageIds = JsonSerializer.Serialize(imageIds);
        await _context.SaveChangesAsync();

        return Ok(image);
    }

    // GET: api/Barbers/{barberId}/services
    [HttpGet("{barberId}/services")]
    public async Task<ActionResult<IEnumerable<object>>> GetBarberServices(int barberId)
    {
        var barber = await _context.Barbers.FindAsync(barberId);
        if (barber == null) return NotFound();

        var services = await _context.BarberSpecialties
            .Include(bs => bs.Service)
            .Where(bs => bs.BarberId == barberId && !bs.IsDeleted)
            .Select(bs => new {
                bs.Id,
                bs.ServiceId,
                ServiceName = bs.Service.Name,
                ServicePrice = bs.Service.Price,
                ServiceDuration = bs.Service.DurationMinutes,
                bs.ExpertiseLevel,
                bs.IsPrimary,
                bs.Notes
            })
            .ToListAsync();

        return Ok(services);
    }

    // POST: api/Barbers/{barberId}/services
    [HttpPost("{barberId}/services")]
    public async Task<IActionResult> AssignBarberServices(int barberId, [FromBody] List<int> serviceIds)
    {
        var barber = await _context.Barbers.FindAsync(barberId);
        if (barber == null) return NotFound();

        // Get existing assignments
        var existing = await _context.BarberSpecialties
            .Where(bs => bs.BarberId == barberId && !bs.IsDeleted)
            .Select(bs => bs.ServiceId)
            .ToListAsync();

        // Add new ones
        var toAdd = serviceIds.Except(existing).ToList();
        foreach (var serviceId in toAdd)
        {
            var service = await _context.Services.FindAsync(serviceId);
            if (service == null) continue;

            _context.BarberSpecialties.Add(new BarberSpecialty
            {
                BarberId = barberId,
                ServiceId = serviceId,
                ExpertiseLevel = 3,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Remove unchecked ones
        var toRemove = existing.Except(serviceIds).ToList();
        var specialtiesToRemove = await _context.BarberSpecialties
            .Where(bs => bs.BarberId == barberId && toRemove.Contains(bs.ServiceId) && !bs.IsDeleted)
            .ToListAsync();

        foreach (var specialty in specialtiesToRemove)
        {
            specialty.IsDeleted = true;
            specialty.DeletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    // DELETE: api/Barbers/{barberId}/services/{serviceId}
    [HttpDelete("{barberId}/services/{serviceId}")]
    public async Task<IActionResult> RemoveBarberService(int barberId, int serviceId)
    {
        var specialty = await _context.BarberSpecialties
            .FirstOrDefaultAsync(bs => bs.BarberId == barberId && bs.ServiceId == serviceId && !bs.IsDeleted);

        if (specialty == null) return NotFound();

        specialty.IsDeleted = true;
        specialty.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    public class CreateBarberDto
    {
        public int SalonId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Bio { get; set; }
    }

    public class UpdateBarberDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Password { get; set; }
        public string? Bio { get; set; }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<object>> UpdateBarber(int id, UpdateBarberDto dto)
    {
        var barber = await _context.Barbers
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

        if (barber == null) return NotFound("Frizer nije pronađen.");

        var existingUser = await _context.Users.AnyAsync(u => 
            (u.Username == dto.Username || u.Email == dto.Email) && u.Id != barber.UserId && !u.IsDeleted);
        if (existingUser) throw new UserException("Korisničko ime ili email su već u upotrebi.");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            barber.Bio = dto.Bio;
            barber.UpdatedAt = DateTime.UtcNow;

            if (barber.User != null)
            {
                barber.User.FirstName = dto.FirstName;
                barber.User.LastName = dto.LastName;
                barber.User.Email = dto.Email;
                barber.User.Username = dto.Username;
                barber.User.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(dto.Password))
                {
                    if (dto.Password.Length < 6) throw new UserException("Lozinka mora imati najmanje 6 karaktera.");
                    if (BCrypt.Net.BCrypt.Verify(dto.Password, barber.User.PasswordHash))
                        throw new UserException("Nova lozinka ne može biti ista kao stara.");
                    
                    barber.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                }
            }

            await _context.SaveChangesAsync();

            var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(currentUserIdString, out int currentUserId))
            {
                var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == currentUserId);
                if (admin != null)
                {
                    _context.AdminLogs.Add(new AdminLog
                    {
                        AdminId = admin.Id,
                        Action = "Update Barber",
                        EntityType = "Barber",
                        EntityId = barber.Id,
                        Notes = $"Ažuriran frizer. Username: {dto.Username}",
                        CreatedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                }
            }

            await transaction.CommitAsync();

            return Ok(new {
                barber.Id,
                barber.UserId,
                barber.SalonId,
                barber.Rating,
                barber.Bio,
                barber.ImageIds,
                FirstName = barber.User?.FirstName,
                LastName = barber.User?.LastName,
                Email = barber.User?.Email,
                Username = barber.User?.Username
            });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
