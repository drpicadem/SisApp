using Microsoft.AspNetCore.Mvc;
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
}
