using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Constants;
using ŠišAppApi.Data;
using ŠišAppApi.Filters;
using ŠišAppApi.Models;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Services.Services;

public class ImageService : IImageService
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    internal static readonly byte[] SeedImageBytes = DbInitializer.SeedImageBytes;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ImageService> _logger;
    private readonly ICurrentUserService _currentUser;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"
    };

    private static readonly Dictionary<string, HashSet<string>> AllowedMimeByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/pjpeg" },
        [".jpeg"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/pjpeg" },
        [".png"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "image/png" },
        [".gif"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "image/gif" },
        [".webp"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "image/webp" },
        [".bmp"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "image/bmp", "image/x-ms-bmp" }
    };

    private const long MaxFileSize = 10 * 1024 * 1024;

    public ImageService(
        ApplicationDbContext context,
        ILogger<ImageService> logger,
        ICurrentUserService currentUser)
    {
        _context = context;
        _logger = logger;
        _currentUser = currentUser;
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

        ValidateMimeType(file, extension);
        await ValidateMagicBytesAsync(file, extension);
        await EnsureUploadOwnershipAsync(entityId, entityType);

        var fileName = $"{Guid.NewGuid()}{extension}";
        byte[] fileBytes;
        await using (var inputStream = file.OpenReadStream())
        await using (var memoryStream = new MemoryStream())
        {
            await inputStream.CopyToAsync(memoryStream);
            fileBytes = memoryStream.ToArray();
        }


        var image = new Image
        {
            Id = Guid.NewGuid().ToString(),
            FileName = fileName,
            ContentType = file.ContentType,
            FileSize = fileBytes.Length,
            FileData = fileBytes,
            Url = string.Empty,
            ImageType = imageType,
            EntityId = entityId,
            EntityType = entityType,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        image.Url = $"/api/Images/file/{image.Id}";

        _context.Images.Add(image);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Image uploaded: {image.Id} -> {image.Url}");
        return image;
    }

    private async Task EnsureUploadOwnershipAsync(int? entityId, string? entityType)
    {
        if (!_currentUser.UserId.HasValue)
            throw new UserException("Korisnik nije autentificiran.");

        if (!entityId.HasValue || entityId.Value <= 0 || string.IsNullOrWhiteSpace(entityType))
            throw new UserException("Upload zahtijeva validan entityId i entityType.");

        var currentUserId = _currentUser.UserId.Value;
        var isAdmin = string.Equals(_currentUser.Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase);
        var normalizedType = entityType.Trim();

        switch (normalizedType)
        {
            case "User":
                if (!isAdmin && entityId.Value != currentUserId)
                    throw new UserException("Možete uploadovati samo slike za svoj profil.");
                return;

            case "Barber":
                var barber = await _context.Barbers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == entityId.Value && !b.IsDeleted);

                if (barber == null)
                    throw new UserException("Frizer nije pronađen.");

                if (!isAdmin && barber.UserId != currentUserId)
                    throw new UserException("Možete uploadovati samo slike za svoj barber profil.");
                return;

            case "Salon":
                var salonExists = await _context.Salons
                    .AsNoTracking()
                    .AnyAsync(s => s.Id == entityId.Value && !s.IsDeleted);

                if (!salonExists)
                    throw new UserException("Salon nije pronađen.");

                if (isAdmin)
                    return;

                var ownsSalonAsBarber = await _context.Barbers
                    .AsNoTracking()
                    .AnyAsync(b => b.SalonId == entityId.Value && b.UserId == currentUserId && !b.IsDeleted);

                if (!ownsSalonAsBarber)
                    throw new UserException("Možete uploadovati samo slike za salon kojem pripadate.");
                return;

            default:
                throw new UserException($"Nepodržan entityType: {normalizedType}.");
        }
    }

    public async Task<Image?> GetByIdAsync(string id)
    {
        var image = await _context.Images
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
        if (image == null)
            return null;

        await EnsureCanAccessImageAsync(image);
        return image;
    }

    public async Task<List<Image>> GetByEntityAsync(int entityId, string entityType, int page = 1, int pageSize = DefaultPageSize)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        var skip = (normalizedPage - 1) * normalizedPageSize;

        await EnsureCanAccessEntityAsync(entityId, entityType);
        return await _context.Images
            .Where(i => i.EntityId == entityId && i.EntityType == entityType && !i.IsDeleted && i.IsActive)
            .OrderBy(i => i.DisplayOrder)
            .Skip(skip)
            .Take(normalizedPageSize)
            .ToListAsync();
    }

    public async Task<(byte[] Content, string ContentType, string FileName)?> GetFileForDownloadAsync(string id)
    {
        var image = await _context.Images
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted && i.IsActive);
        if (image == null)
            return null;

        await EnsureCanAccessImageAsync(image);

        var bytes = image.FileData is { Length: > 0 } ? image.FileData : SeedImageBytes;
        var contentType = string.IsNullOrWhiteSpace(image.ContentType) ? "application/octet-stream" : image.ContentType;
        var fileName = string.IsNullOrWhiteSpace(image.FileName) ? "image" : image.FileName;
        return (bytes, contentType, fileName);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var image = await _context.Images.FirstOrDefaultAsync(i => i.Id == id);
        if (image == null)
            throw new UserException("Slika nije pronađena");

        await EnsureDeleteOwnershipAsync(image);

        image.IsDeleted = true;
        image.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();


        return true;
    }

    private async Task EnsureDeleteOwnershipAsync(Image image)
    {
        if (!_currentUser.UserId.HasValue)
            throw new UserException("Korisnik nije autentificiran.");

        var currentUserId = _currentUser.UserId.Value;
        var isAdmin = string.Equals(_currentUser.Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase);

        if (isAdmin)
            return;

        if (!image.EntityId.HasValue || string.IsNullOrWhiteSpace(image.EntityType))
            throw new UserException("Brisanje nije dozvoljeno za sliku bez ownership metapodataka.");

        switch (image.EntityType.Trim())
        {
            case "User":
                if (image.EntityId.Value != currentUserId)
                    throw new UserException("Možete brisati samo svoje profilne slike.");
                return;

            case "Barber":
                var barberOwned = await _context.Barbers
                    .AsNoTracking()
                    .AnyAsync(b => b.Id == image.EntityId.Value && b.UserId == currentUserId && !b.IsDeleted);

                if (!barberOwned)
                    throw new UserException("Možete brisati samo slike svog barber profila.");
                return;

            case "Salon":
                var salonOwned = await _context.Barbers
                    .AsNoTracking()
                    .AnyAsync(b => b.SalonId == image.EntityId.Value && b.UserId == currentUserId && !b.IsDeleted);

                if (!salonOwned)
                    throw new UserException("Možete brisati samo slike salona kojem pripadate.");
                return;

            default:
                throw new UserException("Brisanje nije dozvoljeno za ovaj tip slike.");
        }
    }

    private async Task EnsureCanAccessImageAsync(Image image)
    {
        if (!image.EntityId.HasValue || string.IsNullOrWhiteSpace(image.EntityType))
            throw new UserException("Pristup slici nije dozvoljen.");

        await EnsureCanAccessEntityAsync(image.EntityId.Value, image.EntityType);
    }

    private async Task EnsureCanAccessEntityAsync(int entityId, string? entityType)
    {
        if (!_currentUser.UserId.HasValue)
            throw new UserException("Korisnik nije autentificiran.");

        if (entityId <= 0 || string.IsNullOrWhiteSpace(entityType))
            throw new UserException("Neispravan entitet slike.");

        var currentUserId = _currentUser.UserId.Value;
        var isAdmin = string.Equals(_currentUser.Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase);

        if (isAdmin)
            return;

        switch (entityType.Trim())
        {
            case "User":
                if (entityId != currentUserId)
                    throw new UserException("Nemate pristup ovoj slici.");
                return;

            case "Barber":
            case "Salon":
            case "ServiceCategory":
            case "SalonAmenity":
            case "Review":
            case "Service":
                return;

            default:
                throw new UserException("Nepodržan entityType.");
        }
    }

    private static void ValidateMimeType(IFormFile file, string extension)
    {
        var contentType = file.ContentType?.Trim();
        if (string.IsNullOrWhiteSpace(contentType))
            throw new UserException("MIME tip fajla nije validan.");

        if (!AllowedMimeByExtension.TryGetValue(extension, out var allowedMimes) || !allowedMimes.Contains(contentType))
            throw new UserException("MIME tip fajla nije dozvoljen.");
    }

    private static async Task ValidateMagicBytesAsync(IFormFile file, string extension)
    {
        await using var stream = file.OpenReadStream();
        var header = new byte[16];
        var read = await stream.ReadAsync(header, 0, header.Length);
        if (read <= 0)
            throw new UserException("Neispravan sadržaj fajla.");

        var valid = extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => HasPrefix(header, read, 0xFF, 0xD8, 0xFF),
            ".png" => HasPrefix(header, read, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A),
            ".gif" => HasAsciiPrefix(header, read, "GIF87a") || HasAsciiPrefix(header, read, "GIF89a"),
            ".bmp" => HasPrefix(header, read, 0x42, 0x4D),
            ".webp" => HasAsciiPrefix(header, read, "RIFF") && HasAsciiAtOffset(header, read, 8, "WEBP"),
            _ => false
        };

        if (!valid)
            throw new UserException("Sadržaj fajla ne odgovara ekstenziji.");
    }

    private static bool HasPrefix(byte[] data, int read, params byte[] expected)
    {
        if (read < expected.Length)
            return false;

        for (var i = 0; i < expected.Length; i++)
        {
            if (data[i] != expected[i])
                return false;
        }

        return true;
    }

    private static bool HasAsciiPrefix(byte[] data, int read, string ascii)
    {
        var expected = System.Text.Encoding.ASCII.GetBytes(ascii);
        return HasPrefix(data, read, expected);
    }

    private static bool HasAsciiAtOffset(byte[] data, int read, int offset, string ascii)
    {
        var expected = System.Text.Encoding.ASCII.GetBytes(ascii);
        if (read < offset + expected.Length)
            return false;

        for (var i = 0; i < expected.Length; i++)
        {
            if (data[offset + i] != expected[i])
                return false;
        }

        return true;
    }

}
