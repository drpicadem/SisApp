using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Constants;
using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Models.Requests;
using ŠišAppApi.Models.SearchObjects;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ServicesController : BaseCRUDController<ServiceDto, ServiceSearchObject, ServiceInsertRequest, ServiceUpdateRequest>
{
    private readonly IServiceService _serviceService;

    public ServicesController(IServiceService serviceService, ICurrentUserService currentUser) : base(serviceService, currentUser)
    {
        _serviceService = serviceService;
    }

    [HttpGet("salon/{salonId}")]
    public async Task<ActionResult<IEnumerable<ServiceDto>>> GetServicesBySalon(int salonId)
    {
        var search = new ServiceSearchObject { SalonId = salonId, IsDeleted = false };
        return Ok(await _serviceService.Get(search));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.AdminOrBarber)]
    public override async Task<ActionResult<ServiceDto>> Insert([FromBody] ServiceInsertRequest request)
    {
        return await base.Insert(request);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = AppRoles.AdminOrBarber)]
    public override async Task<ActionResult<ServiceDto>> Update(int id, [FromBody] ServiceUpdateRequest request)
    {
        return await base.Update(id, request);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = AppRoles.AdminOrBarber)]
    public override async Task<ActionResult<ServiceDto>> Delete(int id)
    {
        var userRole = GetUserRole();
        if (userRole == AppRoles.Barber)
        {
            var userId = GetUserId();
            var result = await _serviceService.DeleteAsBarber(id, userId);
            return Ok(result);
        }
        var adminResult = await _serviceService.Delete(id);
        return Ok(adminResult);
    }
}