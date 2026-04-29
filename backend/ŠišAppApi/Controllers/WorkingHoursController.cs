using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Filters;
using ŠišAppApi.Models;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WorkingHoursController : ControllerBase
{
    private const int DefaultPageSize = 20;
    private readonly IWorkingHoursService _workingHoursService;
    private readonly ICurrentUserService _currentUser;
    
    public record WorkingHoursDto(
        int Id,
        int BarberId,
        int DayOfWeek,
        TimeSpan StartTime,
        TimeSpan EndTime,
        bool IsWorking,
        bool IsDefault,
        DateTime? ValidFrom,
        DateTime? ValidTo,
        string? Notes,
        DateTime CreatedAt,
        DateTime? UpdatedAt);

    public WorkingHoursController(IWorkingHoursService workingHoursService, ICurrentUserService currentUser)
    {
        _workingHoursService = workingHoursService;
        _currentUser = currentUser;
    }

    private int GetUserId()
    {
        if (!_currentUser.UserId.HasValue)
            throw new UserException("Korisnik nije autentificiran.");
        return _currentUser.UserId.Value;
    }

    [HttpGet("my-schedule")]
    public async Task<ActionResult<IEnumerable<WorkingHoursDto>>> GetMySchedule(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize)
    {
        var schedule = await _workingHoursService.GetMyScheduleAsync(GetUserId(), page, pageSize);
        return Ok(schedule.Select(ToDto));
    }

    [HttpGet("barber/{barberId}")]
    public async Task<ActionResult<IEnumerable<WorkingHoursDto>>> GetBarberSchedule(
        int barberId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize)
    {
        var schedule = await _workingHoursService.GetBarberScheduleAsync(barberId, page, pageSize);
        return Ok(schedule.Select(ToDto));
    }

    [HttpPost]
    public async Task<ActionResult<WorkingHoursDto>> CreateWorkingHours([FromBody] WorkingHoursCreateDto dto)
    {
        var result = await _workingHoursService.CreateWorkingHoursAsync(GetUserId(), new WorkingHoursUpsertRequest
        {
            DayOfWeek = dto.DayOfWeek,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            IsWorking = dto.IsWorking,
            Notes = dto.Notes
        });
        return Ok(ToDto(result));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<WorkingHoursDto>> UpdateWorkingHours(int id, [FromBody] WorkingHoursCreateDto dto)
    {
        var result = await _workingHoursService.UpdateWorkingHoursAsync(id, GetUserId(), new WorkingHoursUpsertRequest
        {
            DayOfWeek = dto.DayOfWeek,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            IsWorking = dto.IsWorking,
            Notes = dto.Notes
        });
        return Ok(ToDto(result));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWorkingHours(int id)
    {
        await _workingHoursService.DeleteWorkingHoursAsync(id, GetUserId());
        return Ok(new { message = "Radno vrijeme obrisano." });
    }

    private static WorkingHoursDto ToDto(WorkingHours workingHours) =>
        new(
            workingHours.Id,
            workingHours.BarberId,
            workingHours.DayOfWeek,
            workingHours.StartTime,
            workingHours.EndTime,
            workingHours.IsWorking,
            workingHours.IsDefault,
            workingHours.ValidFrom,
            workingHours.ValidTo,
            workingHours.Notes,
            workingHours.CreatedAt,
            workingHours.UpdatedAt);
}

public class WorkingHoursCreateDto
{
    public int DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsWorking { get; set; } = true;
    public string? Notes { get; set; }
}
