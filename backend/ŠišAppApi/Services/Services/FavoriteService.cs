using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Data;
using ŠišAppApi.Filters;
using ŠišAppApi.Models;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Services.Services
{
    public class FavoriteService : IFavoriteService
    {
        private readonly ApplicationDbContext _context;

        public FavoriteService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<int>> GetFavoriteSalonIdsAsync(int userId, int page, int pageSize)
        {
            var normalizedPage = page < 1 ? 1 : page;
            var normalizedPageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
            var skip = (normalizedPage - 1) * normalizedPageSize;

            return await _context.FavoriteSalons
                .Where(f => f.UserId == userId)
                .OrderBy(f => f.SalonId)
                .Skip(skip)
                .Take(normalizedPageSize)
                .Select(f => f.SalonId)
                .ToListAsync();
        }

        public async Task<bool> ToggleFavoriteSalonAsync(int userId, int salonId)
        {
            var salon = await _context.Salons.FindAsync(salonId);
            if (salon == null)
                throw new NotFoundException("Salon not found");

            var existing = await _context.FavoriteSalons
                .FirstOrDefaultAsync(f => f.UserId == userId && f.SalonId == salonId);

            if (existing != null)
            {
                _context.FavoriteSalons.Remove(existing);
                await _context.SaveChangesAsync();
                return false;
            }

            _context.FavoriteSalons.Add(new FavoriteSalon
            {
                UserId = userId,
                SalonId = salonId,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
