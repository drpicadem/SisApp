using ŠišAppApi.Models;

namespace ŠišAppApi.Services.Interfaces;

public interface IImageService
{
    Task<Image> UploadImageAsync(IFormFile file, string? imageType, int? entityId, string? entityType);
    Task<Image?> GetByIdAsync(string id);
    Task<List<Image>> GetByEntityAsync(int entityId, string entityType);
    Task<bool> DeleteAsync(string id);
}
