using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Data;
using ŠišAppApi.Models;
using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Filters;
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

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim))
            throw new UserException("Korisnik nije autentificiran.");
        return int.Parse(claim);
    }

    private ReviewDto MapToDto(Review r)
    {
        return new ReviewDto
        {
            Id = r.Id,
            UserId = r.UserId,
            UserName = r.User != null ? $"{r.User.FirstName} {r.User.LastName}" : "Anoniman korisnik",
            BarberId = r.BarberId,
            BarberName = r.Barber?.User != null ? $"{r.Barber.User.FirstName} {r.Barber.User.LastName}" : "Nepoznato",
            SalonId = r.SalonId,
            SalonName = r.Salon?.Name,
            AppointmentId = r.AppointmentId,
            ServiceName = r.Appointment?.Service?.Name,
            Rating = r.Rating,
            Comment = r.Comment ?? string.Empty,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            HelpfulCount = r.HelpfulCount,
            IsVerified = r.IsVerified,
            BarberResponse = r.BarberResponse,
            BarberRespondedAt = r.BarberRespondedAt
        };
    }

    private IQueryable<Review> IncludeAll()
    {
        return _context.Reviews
            .Include(r => r.User)
            .Include(r => r.Barber).ThenInclude(b => b.User)
            .Include(r => r.Salon)
            .Include(r => r.Appointment).ThenInclude(a => a.Service);
    }

    [HttpPost]
    public async Task<ActionResult<ReviewDto>> CreateReview([FromBody] CreateReviewDto dto)
    {
        var userId = GetUserId();

        var existing = await _context.Reviews
            .FirstOrDefaultAsync(r => r.AppointmentId == dto.AppointmentId && r.UserId == userId);

        if (existing != null)
            throw new UserException("Recenzija već postoji za ovu rezervaciju.");

        var appointment = await _context.Appointments
            .Include(a => a.Barber)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == dto.AppointmentId);

        if (appointment == null)
            throw new UserException("Termin nije pronađen.");

        if (appointment.Status != "Completed" && appointment.PaymentStatus != "Paid")
            throw new UserException("Možete ostaviti recenziju samo za završene ili plaćene termine.");

        if (appointment.UserId != userId)
            throw new UserException("Možete ostaviti recenziju samo za svoje termine.");

        if (appointment.BarberId != dto.BarberId)
            throw new UserException("Navedeni frizer nije radio ovaj termin.");

        var review = new Review
        {
            AppointmentId = dto.AppointmentId,
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

        // Update barber average rating
        var barber = await _context.Barbers.FindAsync(dto.BarberId);
        if (barber != null)
        {
            var barberReviews = await _context.Reviews
                .Where(r => r.BarberId == dto.BarberId)
                .ToListAsync();
            var allRatings = barberReviews.Select(r => r.Rating).Append(dto.Rating).ToList();
            barber.Rating = allRatings.Average();
            barber.ReviewCount = allRatings.Count;
        }

        // Update salon average rating
        var salon = await _context.Salons.FindAsync(appointment.SalonId);
        if (salon != null)
        {
            var salonReviews = await _context.Reviews
                .Where(r => r.SalonId == appointment.SalonId)
                .ToListAsync();
            var allRatings = salonReviews.Select(r => r.Rating).Append(dto.Rating).ToList();
            salon.Rating = allRatings.Average();
        }

        await _context.SaveChangesAsync();

        var created = await IncludeAll().FirstOrDefaultAsync(r => r.Id == review.Id);
        return Ok(MapToDto(created!));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ReviewDto>> UpdateReview(int id, [FromBody] CreateReviewDto dto)
    {
        var userId = GetUserId();

        var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id);
        if (review == null)
            throw new UserException("Recenzija nije pronađena.");

        if (review.UserId != userId)
            throw new UserException("Možete ažurirati samo svoje recenzije.");

        review.Rating = dto.Rating;
        review.Comment = dto.Comment;
        review.UpdatedAt = DateTime.UtcNow;

        // Recalculate barber rating
        var barber = await _context.Barbers.FindAsync(review.BarberId);
        if (barber != null)
        {
            var barberReviews = await _context.Reviews
                .Where(r => r.BarberId == review.BarberId)
                .ToListAsync();
            barber.Rating = barberReviews.Select(r => r.Id == id ? dto.Rating : r.Rating).Average();
            barber.ReviewCount = barberReviews.Count;
        }

        if (review.SalonId.HasValue)
        {
            var salon = await _context.Salons.FindAsync(review.SalonId);
            if (salon != null)
            {
                var salonReviews = await _context.Reviews
                    .Where(r => r.SalonId == review.SalonId)
                    .ToListAsync();
                salon.Rating = salonReviews.Select(r => r.Id == id ? dto.Rating : r.Rating).Average();
            }
        }

        await _context.SaveChangesAsync();

        var updated = await IncludeAll().FirstOrDefaultAsync(r => r.Id == id);
        return Ok(MapToDto(updated!));
    }

    [HttpGet("my-reviews")]
    public async Task<ActionResult<List<ReviewDto>>> GetMyReviews()
    {
        var userId = GetUserId();

        var reviews = await IncludeAll()
            .Where(r => r.UserId == userId && !r.IsHidden)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(reviews.Select(MapToDto).ToList());
    }

    /// <summary>
    /// Get reviews for the currently logged-in barber
    /// </summary>
    [HttpGet("barber-reviews")]
    public async Task<ActionResult<List<ReviewDto>>> GetMyBarberReviews()
    {
        var userId = GetUserId();

        var barber = await _context.Barbers.FirstOrDefaultAsync(b => b.UserId == userId);
        if (barber == null)
            throw new UserException("Niste registrirani kao frizer.");

        var reviews = await IncludeAll()
            .Where(r => r.BarberId == barber.Id && !r.IsHidden)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(reviews.Select(MapToDto).ToList());
    }

    /// <summary>
    /// Barber responds to a review
    /// </summary>
    [HttpPut("{id}/respond")]
    public async Task<ActionResult<ReviewDto>> RespondToReview(int id, [FromBody] ReviewResponseDto dto)
    {
        var userId = GetUserId();

        var barber = await _context.Barbers.FirstOrDefaultAsync(b => b.UserId == userId);
        if (barber == null)
            throw new UserException("Niste registrirani kao frizer.");

        var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id);
        if (review == null)
            throw new UserException("Recenzija nije pronađena.");

        if (review.BarberId != barber.Id)
            throw new UserException("Možete odgovoriti samo na svoje recenzije.");

        review.BarberResponse = dto.Response;
        review.BarberRespondedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var updated = await IncludeAll().FirstOrDefaultAsync(r => r.Id == id);
        return Ok(MapToDto(updated!));
    }

    [AllowAnonymous]
    [HttpGet("barber/{barberId}")]
    public async Task<ActionResult<List<ReviewDto>>> GetReviewsForBarber(int barberId)
    {
        var reviews = await IncludeAll()
            .Where(r => r.BarberId == barberId && !r.IsHidden)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(reviews.Select(MapToDto).ToList());
    }

    [AllowAnonymous]
    [HttpGet("salon/{salonId}")]
    public async Task<ActionResult<List<ReviewDto>>> GetReviewsForSalon(int salonId)
    {
        var reviews = await IncludeAll()
            .Where(r => r.SalonId == salonId && !r.IsHidden)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(reviews.Select(MapToDto).ToList());
    }

    [HttpPost("{id}/helpful")]
    public async Task<IActionResult> MarkAsHelpful(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
            throw new UserException("Recenzija nije pronađena.");

        review.HelpfulCount++;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Recenzija označena kao korisna." });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/verify")]
    public async Task<IActionResult> VerifyReview(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
            throw new UserException("Recenzija nije pronađena.");

        review.IsVerified = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Recenzija je verificirana." });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/hide")]
    public async Task<IActionResult> HideReview(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
            throw new UserException("Recenzija nije pronađena.");

        review.IsHidden = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Recenzija je sakrivena." });
    }
}