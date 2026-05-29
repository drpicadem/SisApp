using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Constants;
using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Models.Requests;
using ŠišAppApi.Models.SearchObjects;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalonAmenityController : BaseCRUDController<SalonAmenityDto, SalonAmenitySearchObject, SalonAmenityInsertRequest, SalonAmenityUpdateRequest>
{
    public SalonAmenityController(ISalonAmenityService service, ICurrentUserService currentUser) : base(service, currentUser)
    {
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public override async Task<ActionResult<SalonAmenityDto>> Insert([FromBody] SalonAmenityInsertRequest request)
    {
        return await base.Insert(request);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = AppRoles.Admin)]
    public override async Task<ActionResult<SalonAmenityDto>> Update(int id, [FromBody] SalonAmenityUpdateRequest request)
    {
        return await base.Update(id, request);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = AppRoles.Admin)]
    public override async Task<ActionResult<SalonAmenityDto>> Delete(int id)
    {
        return await base.Delete(id);
    }
}
