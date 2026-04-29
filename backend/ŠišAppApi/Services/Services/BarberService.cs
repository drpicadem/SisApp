using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ŠišAppApi.Constants;
using ŠišAppApi.Data;
using ŠišAppApi.Filters;
using ŠišAppApi.Models;
using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Services.Services
{
    public class BarberService : IBarberService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAdminLogService _adminLogService;

        public BarberService(ApplicationDbContext context, IAdminLogService adminLogService)
        {
            _context = context;
            _adminLogService = adminLogService;
        }

        public async Task<BarberProfileDto?> GetMyProfileAsync(int userId)
        {
            var barber = await _context.Barbers
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.UserId == userId && !b.IsDeleted);

            return barber == null ? null : MapToDto(barber);
        }

        public async Task<IEnumerable<BarberProfileDto>> GetBarbersBySalonAsync(int salonId, int page, int pageSize, int? serviceId)
        {
            var (p, ps) = Normalize(page, pageSize);

            var query = _context.Barbers
                .Include(b => b.User)
                .Where(b => b.SalonId == salonId && !b.IsDeleted && b.IsAvailable);

            if (serviceId.HasValue)
            {
                query = query.Where(b => _context.BarberSpecialties
                    .Any(bs => bs.BarberId == b.Id && bs.ServiceId == serviceId.Value && !bs.IsDeleted));
            }

            var barbers = await query
                .OrderBy(b => b.Id)
                .Skip((p - 1) * ps)
                .Take(ps)
                .ToListAsync();

            return barbers.Select(b => MapToDto(b));
        }

        public async Task<BarberProfileDto> CreateBarberAsync(CreateBarberRequest dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email || u.Username == dto.Username))
                throw new UserException("Korisničko ime ili email već postoje.");

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = AppRoles.Barber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                IsEmailVerified = true
            };

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var barber = new Barber
                {
                    UserId = user.Id,
                    SalonId = dto.SalonId,
                    Bio = dto.Bio,
                    CreatedAt = DateTime.UtcNow,
                    Rating = 5.0
                };

                _context.Barbers.Add(barber);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return MapToDto(barber, user);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateBarberImageAsync(int barberId, string imageId)
        {
            var barber = await _context.Barbers.FindAsync(barberId);
            if (barber == null)
                throw new NotFoundException("Frizer nije pronađen.");

            var imageIds = string.IsNullOrEmpty(barber.ImageIds)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(barber.ImageIds) ?? new List<string>();

            imageIds.Add(imageId);
            barber.ImageIds = JsonSerializer.Serialize(imageIds);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<BarberServiceItemDto>> GetBarberServicesAsync(int barberId, int page, int pageSize)
        {
            var (p, ps) = Normalize(page, pageSize);

            var barber = await _context.Barbers.FindAsync(barberId);
            if (barber == null)
                throw new NotFoundException("Frizer nije pronađen.");

            return await _context.BarberSpecialties
                .Include(bs => bs.Service)
                .Where(bs => bs.BarberId == barberId && !bs.IsDeleted)
                .OrderBy(bs => bs.Id)
                .Skip((p - 1) * ps)
                .Take(ps)
                .Select(bs => new BarberServiceItemDto
                {
                    Id = bs.Id,
                    ServiceId = bs.ServiceId,
                    ServiceName = bs.Service.Name,
                    ServicePrice = bs.Service.Price,
                    ServiceDuration = bs.Service.DurationMinutes,
                    ExpertiseLevel = bs.ExpertiseLevel,
                    IsPrimary = bs.IsPrimary,
                    Notes = bs.Notes
                })
                .ToListAsync();
        }

        public async Task AssignBarberServicesAsync(int barberId, List<int> serviceIds)
        {
            var barber = await _context.Barbers.FindAsync(barberId);
            if (barber == null)
                throw new NotFoundException("Frizer nije pronađen.");

            var existing = await _context.BarberSpecialties
                .Where(bs => bs.BarberId == barberId && !bs.IsDeleted)
                .Select(bs => bs.ServiceId)
                .ToListAsync();

            var toAdd = serviceIds.Except(existing).ToList();
            var validServiceIds = await _context.Services
                .Where(s => toAdd.Contains(s.Id) && !s.IsDeleted)
                .Select(s => s.Id)
                .ToListAsync();

            foreach (var serviceId in validServiceIds)
            {
                _context.BarberSpecialties.Add(new BarberSpecialty
                {
                    BarberId = barberId,
                    ServiceId = serviceId,
                    ExpertiseLevel = 3,
                    CreatedAt = DateTime.UtcNow
                });
            }

            var toRemove = existing.Except(serviceIds).ToList();
            var specialtiesToRemove = await _context.BarberSpecialties
                .Where(bs => bs.BarberId == barberId && toRemove.Contains(bs.ServiceId) && !bs.IsDeleted)
                .ToListAsync();

            foreach (var specialty in specialtiesToRemove)
            {
                specialty.IsDeleted = true;
                specialty.DeletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task RemoveBarberServiceAsync(int barberId, int serviceId)
        {
            var specialty = await _context.BarberSpecialties
                .FirstOrDefaultAsync(bs => bs.BarberId == barberId && bs.ServiceId == serviceId && !bs.IsDeleted);

            if (specialty == null)
                throw new NotFoundException("Specijalizacija nije pronađena.");

            specialty.IsDeleted = true;
            specialty.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<BarberProfileDto> UpdateBarberAsync(int id, UpdateBarberRequest dto, int? adminUserId,
            string? ipAddress = null, string? userAgent = null)
        {
            var barber = await _context.Barbers
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

            if (barber == null)
                throw new NotFoundException("Frizer nije pronađen.");

            var existingUser = await _context.Users.AnyAsync(u =>
                (u.Username == dto.Username || u.Email == dto.Email) && u.Id != barber.UserId && !u.IsDeleted);
            if (existingUser)
                throw new UserException("Korisničko ime ili email su već u upotrebi.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                barber.Bio = dto.Bio;
                barber.UpdatedAt = DateTime.UtcNow;

                if (barber.User != null)
                {
                    barber.User.FirstName = dto.FirstName;
                    barber.User.LastName = dto.LastName;
                    barber.User.Email = dto.Email;
                    barber.User.Username = dto.Username;
                    barber.User.UpdatedAt = DateTime.UtcNow;

                    if (!string.IsNullOrEmpty(dto.Password))
                    {
                        if (dto.Password.Length < 6)
                            throw new UserException("Lozinka mora imati najmanje 6 karaktera.");
                        if (BCrypt.Net.BCrypt.Verify(dto.Password, barber.User.PasswordHash))
                            throw new UserException("Nova lozinka ne može biti ista kao stara.");

                        barber.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                    }
                }

                await _context.SaveChangesAsync();

                if (adminUserId.HasValue)
                {
                    await _adminLogService.LogAsync(
                        adminUserId.Value,
                        "Update Barber",
                        "Barber",
                        barber.Id,
                        $"Ažuriran frizer. Username: {dto.Username}",
                        ipAddress,
                        userAgent);
                }

                await transaction.CommitAsync();
                return MapToDto(barber);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static BarberProfileDto MapToDto(Barber barber, User? user = null)
        {
            var u = user ?? barber.User;
            return new BarberProfileDto
            {
                Id = barber.Id,
                UserId = barber.UserId,
                SalonId = barber.SalonId,
                Rating = barber.Rating,
                Bio = barber.Bio,
                ImageIds = barber.ImageIds,
                FirstName = u?.FirstName ?? string.Empty,
                LastName = u?.LastName ?? string.Empty,
                Email = u?.Email ?? string.Empty,
                Username = u?.Username ?? string.Empty
            };
        }

        private static (int page, int pageSize) Normalize(int page, int pageSize)
        {
            var p = page < 1 ? 1 : page;
            var ps = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
            return (p, ps);
        }
    }
}
