using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ŠišAppApi.Services;

namespace ŠišAppApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecommendationsController : ControllerBase
{
    private readonly RecommendationService _recommendationService;

    public RecommendationsController(RecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    /// <summary>
    /// GET /api/Recommendations
    /// Returns personalized service recommendations for the authenticated user.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRecommendations()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var recommendations = await _recommendationService.GetRecommendations(userId);
        return Ok(recommendations);
    }
}
