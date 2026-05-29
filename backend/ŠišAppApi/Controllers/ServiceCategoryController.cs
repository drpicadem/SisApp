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
public class ServiceCategoryController : BaseCRUDController<ServiceCategoryDto, ServiceCategorySearchObject, ServiceCategoryInsertRequest, ServiceCategoryUpdateRequest>
{
    public ServiceCategoryController(IServiceCategoryService service, ICurrentUserService currentUser) : base(service, currentUser)
    {
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public override async Task<ActionResult<ServiceCategoryDto>> Insert([FromBody] ServiceCategoryInsertRequest request)
    {
        return await base.Insert(request);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = AppRoles.Admin)]
    public override async Task<ActionResult<ServiceCategoryDto>> Update(int id, [FromBody] ServiceCategoryUpdateRequest request)
    {
        return await base.Update(id, request);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = AppRoles.Admin)]
    public override async Task<ActionResult<ServiceCategoryDto>> Delete(int id)
    {
        return await base.Delete(id);
    }
}
