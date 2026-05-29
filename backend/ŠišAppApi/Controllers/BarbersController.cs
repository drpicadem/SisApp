using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Constants;
using ŠišAppApi.Filters;
using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BarbersController : ControllerBase
{
    private const int DefaultPageSize = 20;
    private readonly IBarberService _barberService;
    private readonly IImageService _imageService;
    private readonly ICurrentUserService _currentUser;

    public BarbersController(IBarberService barberService, IImageService imageService, ICurrentUserService currentUser)
    {
        _barberService = barberService;
        _imageService = imageService;
        _currentUser = currentUser;
    }

    [HttpGet("my-profile")]
    public async Task<ActionResult<BarberProfileDto>> GetMyProfile()
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        var profile = await _barberService.GetMyProfileAsync(_currentUser.UserId.Value);
        if (profile == null)
            return NotFound("Niste prijavljeni kao aktivan frizer.");

        return Ok(profile);
    }

    [HttpGet("salon/{salonId}")]
    public async Task<ActionResult<IEnumerable<BarberProfileDto>>> GetBarbersBySalon(
        int salonId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        [FromQuery] int? serviceId = null)
    {
        var barbers = await _barberService.GetBarbersBySalonAsync(salonId, page, pageSize, serviceId);
        return Ok(barbers);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<BarberProfileDto>> PostBarber(CreateBarberRequest dto)
    {
        var barber = await _barberService.CreateBarberAsync(dto);
        return CreatedAtAction("GetBarbersBySalon", new { salonId = dto.SalonId }, barber);
    }

    [HttpPost("{id}/upload-image")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadBarberImage(int id, IFormFile file)
    {
        var image = await _imageService.UploadImageAsync(file, "barber", id, "Barber");
        await _barberService.UpdateBarberImageAsync(id, image.Id);
        return Ok(image);
    }

    [HttpGet("{barberId}/services")]
    public async Task<ActionResult<IEnumerable<BarberServiceItemDto>>> GetBarberServices(
        int barberId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize)
    {
        var services = await _barberService.GetBarberServicesAsync(barberId, page, pageSize);
        return Ok(services);
    }

    [HttpPost("{barberId}/services")]
    [Authorize(Roles = AppRoles.AdminOrBarber)]
    public async Task<IActionResult> AssignBarberServices(int barberId, [FromBody] List<int> serviceIds)
    {
        var denied = await AuthorizeBarberTargetAsync(barberId);
        if (denied != null)
            return denied;

        await _barberService.AssignBarberServicesAsync(barberId, serviceIds);
        return Ok();
    }

    [HttpDelete("{barberId}/services/{serviceId}")]
    [Authorize(Roles = AppRoles.AdminOrBarber)]
    public async Task<IActionResult> RemoveBarberService(int barberId, int serviceId)
    {
        var denied = await AuthorizeBarberTargetAsync(barberId);
        if (denied != null)
            return denied;

        await _barberService.RemoveBarberServiceAsync(barberId, serviceId);
        return NoContent();
    }

    [HttpPut("{id}")]
    [Authorize(Roles = AppRoles.AdminOrBarber)]
    public async Task<IActionResult> UpdateBarber(int id, UpdateBarberRequest dto)
    {
        var denied = await AuthorizeBarberTargetAsync(id);
        if (denied != null)
            return denied;

        var adminUserIdForLog = string.Equals(_currentUser.Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase)
            ? _currentUser.UserId
            : null;

        var result = await _barberService.UpdateBarberAsync(
            id, dto, adminUserIdForLog,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());
        return Ok(result);
    }

    private async Task<IActionResult?> AuthorizeBarberTargetAsync(int targetBarberId)
    {
        if (string.Equals(_currentUser.Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase))
            return null;

        if (!string.Equals(_currentUser.Role, AppRoles.Barber, StringComparison.OrdinalIgnoreCase))
            return Forbid();

        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        var myBarberId = await _barberService.GetBarberIdByUserIdAsync(_currentUser.UserId.Value);
        if (!myBarberId.HasValue || myBarberId.Value != targetBarberId)
            return Forbid();

        return null;
    }
}
