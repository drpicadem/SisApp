using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ŠišAppApi.Filters;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class FavoritesController : ControllerBase
{
    private const int DefaultPageSize = 20;
    private readonly IFavoriteService _favoriteService;
    private readonly ICurrentUserService _currentUser;

    public FavoritesController(IFavoriteService favoriteService, ICurrentUserService currentUser)
    {
        _favoriteService = favoriteService;
        _currentUser = currentUser;
    }

    private int GetCurrentUserId()
    {
        if (_currentUser.UserId.HasValue)
            return _currentUser.UserId.Value;
        throw new UnauthorizedAccessException("User ID not found in token");
    }

    [HttpGet("salons")]
    public async Task<ActionResult<IEnumerable<int>>> GetFavoriteSalonIds(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize)
    {
        try
        {
            var ids = await _favoriteService.GetFavoriteSalonIdsAsync(GetCurrentUserId(), page, pageSize);
            return Ok(ids);
        }
        catch (Exception)
        {
            return Unauthorized(new { message = "Neuspješna autorizacija." });
        }
    }

    [HttpPost("toggle/{salonId}")]
    public async Task<IActionResult> ToggleFavoriteSalon(int salonId)
    {
        try
        {
            var isFavorite = await _favoriteService.ToggleFavoriteSalonAsync(GetCurrentUserId(), salonId);
            return Ok(new { isFavorite });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception)
        {
            return Unauthorized(new { message = "Neuspješna autorizacija." });
        }
    }
}
