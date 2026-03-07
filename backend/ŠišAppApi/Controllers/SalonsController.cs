using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Models;
using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Models.Requests;
using ŠišAppApi.Models.SearchObjects;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SalonsController : BaseCRUDController<SalonDto, SalonSearchObject, SalonInsertRequest, SalonUpdateRequest>
{
    private readonly ISalonService _salonService;
    private readonly IImageService _imageService;

    public SalonsController(ISalonService salonService, IImageService imageService) : base(salonService)
    {
        _salonService = salonService;
        _imageService = imageService;
    }

    // PUT: api/Salons/5/status
    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var result = await _salonService.ToggleStatusAsync(id);
        return Ok(new { isActive = result.IsActive });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public override async Task<ActionResult<SalonDto>> Insert([FromBody] SalonInsertRequest request)
    {
        return await base.Insert(request);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public override async Task<ActionResult<SalonDto>> Update(int id, [FromBody] SalonUpdateRequest request)
    {
        return await base.Update(id, request);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public override async Task<ActionResult<SalonDto>> Delete(int id)
    {
        return await base.Delete(id);
    }

    // POST: api/Salons/5/upload-image
    [HttpPost("{id}/upload-image")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Image>> UploadSalonImage(int id, IFormFile file)
    {
        var salon = await _salonService.GetById(id);
        if (salon == null) return NotFound();

        var image = await _imageService.UploadImageAsync(file, "salon", id, "Salon");
        await _salonService.UpdateSalonImageAsync(id, image.Id);

        return Ok(image);
    }
}