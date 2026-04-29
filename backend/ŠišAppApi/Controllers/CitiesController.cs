using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CitiesController : ControllerBase
{
    private readonly ICityService _cityService;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public CitiesController(ICityService cityService, IMemoryCache cache)
    {
        _cityService = cityService;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        var normalizedQuery = string.IsNullOrWhiteSpace(q) ? string.Empty : q.Trim().ToLowerInvariant();
        var cacheKey = $"cities:get:{normalizedQuery}:{page}:{pageSize}";

        if (_cache.TryGetValue(cacheKey, out object? cached) && cached != null)
            return Ok(cached);

        var result = await _cityService.GetCitiesAsync(q, page, pageSize);
        _cache.Set(cacheKey, result, CacheDuration);
        return Ok(result);
    }
}
