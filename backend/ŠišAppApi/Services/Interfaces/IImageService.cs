using ŠišAppApi.Models;

namespace ŠišAppApi.Services.Interfaces;

public interface IImageService
{
    Task<Image> UploadImageAsync(IFormFile file, string? imageType, int? entityId, string? entityType);
    Task<Image?> GetByIdAsync(string id);
    Task<List<Image>> GetByEntityAsync(int entityId, string entityType, int page = 1, int pageSize = 20);
    Task<(byte[] Content, string ContentType, string FileName)?> GetFileForDownloadAsync(string id);
    Task<bool> DeleteAsync(string id);
}
