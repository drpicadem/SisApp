using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;
    private readonly ICurrentUserService _currentUser;

    public RecommendationsController(IRecommendationService recommendationService, ICurrentUserService currentUser)
    {
        _recommendationService = recommendationService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecommendations([FromQuery] int top = 10)
    {
        if (top < 1) top = 10;
        if (top > 50) top = 50;

        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        var recommendations = await _recommendationService.GetRecommendations(_currentUser.UserId.Value);
        return Ok(recommendations.Take(top));
    }
}
