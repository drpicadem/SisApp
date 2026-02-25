using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Data;
using ŠišAppApi.Filters;
using ŠišAppApi.Models;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Services.Services;

public class ImageService : IImageService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ImageService> _logger;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"
    };

    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    public ImageService(ApplicationDbContext context, IWebHostEnvironment env, ILogger<ImageService> logger)
    {
        _context = context;
        _env = env;
        _logger = logger;
    }

    public async Task<Image> UploadImageAsync(IFormFile file, string? imageType, int? entityId, string? entityType)
    {
        if (file == null || file.Length == 0)
            throw new UserException("Fajl nije proslijeđen ili je prazan");

        if (file.Length > MaxFileSize)
            throw new UserException("Maksimalna veličina fajla je 10MB");

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
            throw new UserException($"Dozvoljeni formati su: {string.Join(", ", AllowedExtensions)}");

        // Build folder: wwwroot/uploads/{imageType}/
        var folder = imageType ?? "general";
        var uploadDir = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", folder);
        Directory.CreateDirectory(uploadDir);

        // Generate unique filename
        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadDir, fileName);

        // Save file to disk
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Create Image record
        var image = new Image
        {
            Id = Guid.NewGuid().ToString(),
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            Url = $"/uploads/{folder}/{fileName}",
            ImageType = imageType,
            EntityId = entityId,
            EntityType = entityType,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Images.Add(image);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Image uploaded: {image.Id} -> {image.Url}");
        return image;
    }

    public async Task<Image?> GetByIdAsync(string id)
    {
        return await _context.Images
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
    }

    public async Task<List<Image>> GetByEntityAsync(int entityId, string entityType)
    {
        return await _context.Images
            .Where(i => i.EntityId == entityId && i.EntityType == entityType && !i.IsDeleted && i.IsActive)
            .OrderBy(i => i.DisplayOrder)
            .ToListAsync();
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var image = await _context.Images.FirstOrDefaultAsync(i => i.Id == id);
        if (image == null)
            throw new UserException("Slika nije pronađena");

        // Soft delete in DB
        image.IsDeleted = true;
        image.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Delete file from disk
        var filePath = Path.Combine(_env.ContentRootPath, "wwwroot", image.Url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation($"File deleted: {filePath}");
        }

        return true;
    }
}
