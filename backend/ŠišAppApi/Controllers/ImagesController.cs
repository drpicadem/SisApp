using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Models;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImagesController : ControllerBase
{
    private readonly IImageService _imageService;

    public ImagesController(IImageService imageService)
    {
        _imageService = imageService;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
    public async Task<ActionResult<Image>> Upload(
        IFormFile file,
        [FromQuery] string? imageType,
        [FromQuery] int? entityId,
        [FromQuery] string? entityType)
    {
        var image = await _imageService.UploadImageAsync(file, imageType, entityId, entityType);
        return Ok(image);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Image>> GetById(string id)
    {
        var image = await _imageService.GetByIdAsync(id);
        if (image == null) return NotFound();
        return Ok(image);
    }

    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<ActionResult<List<Image>>> GetByEntity(string entityType, int entityId)
    {
        var images = await _imageService.GetByEntityAsync(entityId, entityType);
        return Ok(images);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        await _imageService.DeleteAsync(id);
        return NoContent();
    }
}
