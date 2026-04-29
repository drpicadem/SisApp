using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Constants;
using ŠišAppApi.Filters;
using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private const int DefaultPageSize = 20;
    private readonly IReviewService _reviewService;
    private readonly ICurrentUserService _currentUser;

    public ReviewsController(IReviewService reviewService, ICurrentUserService currentUser)
    {
        _reviewService = reviewService;
        _currentUser = currentUser;
    }

    private int GetUserId()
    {
        if (!_currentUser.UserId.HasValue)
            throw new UserException("Korisnik nije autentificiran.");
        return _currentUser.UserId.Value;
    }

    [Authorize(Roles = AppRoles.User)]
    [HttpPost]
    public async Task<ActionResult<ReviewDto>> CreateReview([FromBody] CreateReviewDto dto)
    {
        try
        {
            var result = await _reviewService.CreateReviewAsync(GetUserId(), dto);
            return Ok(result);
        }
        catch (UserException ex)
        {
            return BadRequest(new { code = "BUSINESS_RULE_VIOLATION", userError = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ReviewDto>> UpdateReview(int id, [FromBody] CreateReviewDto dto)
    {
        try
        {
            var result = await _reviewService.UpdateReviewAsync(id, GetUserId(), dto);
            return Ok(result);
        }
        catch (UserException ex)
        {
            return BadRequest(new { code = "BUSINESS_RULE_VIOLATION", userError = ex.Message });
        }
    }

    [HttpGet("my-reviews")]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> GetMyReviews(
        [FromQuery] int page = 1, [FromQuery] int pageSize = DefaultPageSize)
    {
        var reviews = await _reviewService.GetMyReviewsAsync(GetUserId(), page, pageSize);
        return Ok(reviews);
    }

    [HttpGet("barber-reviews")]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> GetMyBarberReviews(
        [FromQuery] int page = 1, [FromQuery] int pageSize = DefaultPageSize)
    {
        var reviews = await _reviewService.GetMyBarberReviewsAsync(GetUserId(), page, pageSize);
        return Ok(reviews);
    }

    [Authorize(Roles = AppRoles.Barber)]
    [HttpPut("{id}/respond")]
    public async Task<ActionResult<ReviewDto>> RespondToReview(int id, [FromBody] ReviewResponseDto dto)
    {
        var result = await _reviewService.RespondToReviewAsync(id, GetUserId(), dto.Response);
        return Ok(result);
    }

    [HttpGet("barber/{barberId}")]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> GetReviewsForBarber(
        int barberId, [FromQuery] int page = 1, [FromQuery] int pageSize = DefaultPageSize)
    {
        var reviews = await _reviewService.GetReviewsForBarberAsync(barberId, page, pageSize);
        return Ok(reviews);
    }

    [HttpGet("salon/{salonId}")]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> GetReviewsForSalon(
        int salonId, [FromQuery] int page = 1, [FromQuery] int pageSize = DefaultPageSize)
    {
        var reviews = await _reviewService.GetReviewsForSalonAsync(salonId, page, pageSize);
        return Ok(reviews);
    }

    [HttpPost("{id}/helpful")]
    public async Task<IActionResult> MarkAsHelpful(int id)
    {
        await _reviewService.MarkAsHelpfulAsync(id);
        return Ok(new { message = "Recenzija označena kao korisna." });
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id}/verify")]
    public async Task<IActionResult> VerifyReview(int id)
    {
        await _reviewService.VerifyReviewAsync(
            id, GetUserId(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());
        return Ok(new { message = "Recenzija je verificirana." });
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id}/hide")]
    public async Task<IActionResult> HideReview(int id, [FromBody] RejectReviewRequest request)
    {
        await _reviewService.HideReviewAsync(
            id, GetUserId(),
            request?.Reason ?? string.Empty,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());
        return Ok(new { message = "Recenzija je sakrivena." });
    }
}

public class RejectReviewRequest
{
    public string Reason { get; set; } = string.Empty;
}
