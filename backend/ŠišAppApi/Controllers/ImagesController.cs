using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ŠišAppApi.Models;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImagesController : ControllerBase
{
    private const int DefaultPageSize = 20;
    private readonly IImageService _imageService;
    
    public record ImageDto(
        string Id,
        string FileName,
        string ContentType,
        long FileSize,
        string Url,
        string? ThumbnailUrl,
        string? AltText,
        int? Width,
        int? Height,
        string? ImageType,
        int? EntityId,
        string? EntityType,
        int DisplayOrder,
        bool IsActive,
        DateTime CreatedAt);

    public ImagesController(IImageService imageService)
    {
        _imageService = imageService;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ImageDto>> Upload(
        IFormFile file,
        [FromQuery] string? imageType,
        [FromQuery] int? entityId,
        [FromQuery] string? entityType)
    {
        var image = await _imageService.UploadImageAsync(file, imageType, entityId, entityType);
        return Ok(ToDto(image));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ImageDto>> GetById(string id)
    {
        var image = await _imageService.GetByIdAsync(id);
        if (image == null) return NotFound();
        return Ok(ToDto(image));
    }

    [HttpGet("file/{id}")]
    public async Task<IActionResult> DownloadFile(string id)
    {
        var file = await _imageService.GetFileForDownloadAsync(id);
        if (file == null)
            return NotFound();

        return File(file.Value.Content, file.Value.ContentType, file.Value.FileName);
    }

    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<ActionResult<List<ImageDto>>> GetByEntity(
        string entityType,
        int entityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize)
    {
        var images = await _imageService.GetByEntityAsync(entityId, entityType, page, pageSize);
        return Ok(images.Select(ToDto).ToList());
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        await _imageService.DeleteAsync(id);
        return NoContent();
    }

    private static ImageDto ToDto(Image image) =>
        new(
            image.Id,
            image.FileName,
            image.ContentType,
            image.FileSize,
            image.Url,
            image.ThumbnailUrl,
            image.AltText,
            image.Width,
            image.Height,
            image.ImageType,
            image.EntityId,
            image.EntityType,
            image.DisplayOrder,
            image.IsActive,
            image.CreatedAt);
}
