using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Data;
using ŠišAppApi.Models;
using System.Security.Cryptography;
using System.Text;

namespace ŠišAppApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BarbersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BarbersController(ApplicationDbContext context)
    {
        _context = context;
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
}
