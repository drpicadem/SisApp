using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Data;
using ŠišAppApi.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ŠišAppApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public FavoritesController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdString, out int userId))
        {
            return userId;
        }
        throw new Exception("User ID not found in token");
    }

    // GET: api/Favorites/salons
    [HttpGet("salons")]
    public async Task<ActionResult<IEnumerable<int>>> GetFavoriteSalonIds()
    {
        try
        {
            var userId = GetCurrentUserId();
            var favoriteSalonIds = await _context.FavoriteSalons
                .Where(f => f.UserId == userId)
                .Select(f => f.SalonId)
                .ToListAsync();

            return Ok(favoriteSalonIds);
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    // POST: api/Favorites/toggle/5
    [HttpPost("toggle/{salonId}")]
    public async Task<IActionResult> ToggleFavoriteSalon(int salonId)
    {
        try
        {
            var userId = GetCurrentUserId();

            var salon = await _context.Salons.FindAsync(salonId);
            if (salon == null)
            {
                return NotFound(new { message = "Salon not found" });
            }

            var existingFavorite = await _context.FavoriteSalons
                .FirstOrDefaultAsync(f => f.UserId == userId && f.SalonId == salonId);

            if (existingFavorite != null)
            {
                // Unfavorite
                _context.FavoriteSalons.Remove(existingFavorite);
                await _context.SaveChangesAsync();
                return Ok(new { isFavorite = false });
            }
            else
            {
                // Favorite
                var newFavorite = new FavoriteSalon
                {
                    UserId = userId,
                    SalonId = salonId,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.FavoriteSalons.Add(newFavorite);
                await _context.SaveChangesAsync();
                return Ok(new { isFavorite = true });
            }
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}
