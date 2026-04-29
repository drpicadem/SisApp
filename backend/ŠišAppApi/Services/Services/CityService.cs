using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Data;
using ŠišAppApi.Models;
using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Services.Services
{
    public class CityService : ICityService
    {
        private readonly ApplicationDbContext _context;

        public CityService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<CityItemDto>> GetCitiesAsync(string? q, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 100;
            if (pageSize > 500) pageSize = 500;

            var normalizedQuery = string.IsNullOrWhiteSpace(q) ? string.Empty : q.Trim().ToLowerInvariant();
            var query = _context.Cities.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(normalizedQuery))
                query = query.Where(c => c.Name.ToLower().Contains(normalizedQuery));

            var totalCount = await query.CountAsync();

            var result = await query
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CityItemDto { Id = c.Id, Name = c.Name })
                .ToListAsync();

            return new PagedResult<CityItemDto>
            {
                Items = result,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
    }
}
