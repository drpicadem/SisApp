using Microsoft.AspNetCore.Mvc;
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
}
