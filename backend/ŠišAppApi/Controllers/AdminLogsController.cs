using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Constants;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[ApiController]
[Route("api/[controller]")]
public class AdminLogsController : ControllerBase
{
    private readonly IAdminLogService _adminLogService;

    public AdminLogsController(IAdminLogService adminLogService)
    {
        _adminLogService = adminLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? action = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _adminLogService.GetLogsAsync(action, from, to, page, pageSize);
        return Ok(result);
    }
}
