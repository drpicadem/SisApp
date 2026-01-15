using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Data;
using ŠišAppApi.Models;
using ŠišAppApi.Models.DTOs;
using System.Security.Claims;

namespace ŠišAppApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReviewsController(ApplicationDbContext context)
    {
        _context = context;
    }

    /* // Override-amo osnovne CRUD metode da ih sakrijemo
    [ApiExplorerSettings(IgnoreApi = true)]
    public override Task<ActionResult<IEnumerable<Review>>> GetAll()
    {
        return base.GetAll();
    }

    [ApiExplorerSettings(IgnoreApi = true), NonAction]
    public override Task<ActionResult<Review>> GetById(int id)
    {
        return base.GetById(id);
    }

    [ApiExplorerSettings(IgnoreApi = true), NonAction]
    public override Task<ActionResult<Review>> Create(Review entity)
    {
        return base.Create(entity);
    }

    [ApiExplorerSettings(IgnoreApi = true), NonAction] 
    public override Task<IActionResult> Update(int id, Review entity)
    {
        return base.Update(id, entity);
    }

    [ApiExplorerSettings(IgnoreApi = true), NonAction]
    public override Task<IActionResult> Delete(int id)
    {
        return base.Delete(id);
    }
 */
    [HttpPost("{appointmentId}/reviews")]
    public async Task<IActionResult> CreateReview(int appointmentId, [FromBody] CreateReviewDto dto)
    {
        try
        {
            // Validacija modela
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Neispravni podaci", errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage) });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { message = "Korisnik nije autentificiran." });

            var userId = int.Parse(userIdClaim);
            
            // Provjeri postoji li već recenzija
            var existing = await _context.Reviews
                .FirstOrDefaultAsync(r => r.AppointmentId == appointmentId && r.UserId == userId);

            if (existing != null)
                return BadRequest(new { message = "Recenzija već postoji za ovu rezervaciju." });

            // Provjeri postoji li termin
            var appointment = await _context.Appointments
                .Include(a => a.Barber)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
                return NotFound(new { message = "Termin nije pronađen." });

            // Provjeri je li termin završen
            if (appointment.Status != "Completed")
                return BadRequest(new { message = "Možete ostaviti recenziju samo za završene termine." });

            // Provjeri je li korisnik stvarno bio na tom terminu
            if (appointment.UserId != userId)
                return BadRequest(new { message = "Možete ostaviti recenziju samo za svoje termine." });

            // Provjeri postoji li frizer
            var barber = await _context.Barbers.FindAsync(dto.BarberId);
            if (barber == null)
                return NotFound(new { message = "Frizer nije pronađen." });

            // Provjeri je li frizer stvarno radio taj termin
            if (appointment.BarberId != dto.BarberId)
                return BadRequest(new { message = "Navedeni frizer nije radio ovaj termin." });

            var review = new Review
            {
                AppointmentId = appointmentId,
                UserId = userId,
                BarberId = dto.BarberId,
                SalonId = appointment.SalonId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                IsVerified = false,
                IsHidden = false,
                HelpfulCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);

            // Ažuriraj prosječnu ocjenu frizera
            var barberReviews = await _context.Reviews
                .Where(r => r.BarberId == dto.BarberId)
                .ToListAsync();

            barber.Rating = (float)barberReviews.Average(r => r.Rating);
            barber.ReviewCount = barberReviews.Count;

            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Recenzija uspješno kreirana",
                review = new {
                    id = review.Id,
                    rating = review.Rating,
                    comment = review.Comment,
                    createdAt = review.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Greška pri kreiranju recenzije", error = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("barber/{barberId}")]
    public async Task<IActionResult> GetReviewsForBarber(int barberId)
    {
        try
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.BarberId == barberId && !r.IsHidden)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    UserName = r.User != null ? $"{r.User.FirstName} {r.User.LastName}" : "Anoniman korisnik",
                    Rating = r.Rating,
                    Comment = r.Comment ?? string.Empty,
                    CreatedAt = r.CreatedAt,
                    HelpfulCount = r.HelpfulCount
                })
                .ToListAsync();

            return Ok(reviews);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Greška pri dohvaćanju recenzija", error = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("salon/{salonId}")]
    public async Task<IActionResult> GetReviewsForSalon(int salonId)
    {
        try
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.SalonId == salonId && !r.IsHidden)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    UserName = r.User != null ? $"{r.User.FirstName} {r.User.LastName}" : "Anoniman korisnik",
                    Rating = r.Rating,
                    Comment = r.Comment ?? string.Empty,
                    CreatedAt = r.CreatedAt,
                    HelpfulCount = r.HelpfulCount
                })
                .ToListAsync();

            return Ok(reviews);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Greška pri dohvaćanju recenzija", error = ex.Message });
        }
    }

    [HttpPost("{id}/helpful")]
    public async Task<IActionResult> MarkAsHelpful(int id)
    {
        try
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound(new { message = "Recenzija nije pronađena." });

            review.HelpfulCount++;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Recenzija označena kao korisna." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Greška pri označavanju recenzije", error = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/verify")]
    public async Task<IActionResult> VerifyReview(int id)
    {
        try
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound(new { message = "Recenzija nije pronađena." });

            review.IsVerified = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Recenzija je verificirana." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Greška pri verifikaciji recenzije", error = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/hide")]
    public async Task<IActionResult> HideReview(int id)
    {
        try
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound(new { message = "Recenzija nije pronađena." });

            review.IsHidden = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Recenzija je sakrivena." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Greška pri sakrivanju recenzije", error = ex.Message });
        }
    }
} 