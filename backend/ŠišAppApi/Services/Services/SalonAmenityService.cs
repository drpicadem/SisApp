using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using ŠišAppApi.Data;
using ŠišAppApi.Models;
using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Models.Requests;
using ŠišAppApi.Models.SearchObjects;
using ŠišAppApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ŠišAppApi.Services.Services;

public class SalonAmenityService : BaseCRUDService<SalonAmenityDto, SalonAmenitySearchObject, SalonAmenity, SalonAmenityInsertRequest, SalonAmenityUpdateRequest>, ISalonAmenityService
{
    private readonly IMemoryCache _cache;
    private const string CacheVersionKey = "salon-amenities:version";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public SalonAmenityService(ApplicationDbContext context, IMapper mapper, IMemoryCache cache) : base(context, mapper)
    {
        _cache = cache;
    }

    public override async Task<IEnumerable<SalonAmenityDto>> Get(SalonAmenitySearchObject search = null)
    {
        var entity = _context.SalonAmenities.AsQueryable();
        var page = Math.Max(1, search?.Page ?? 1);
        var pageSize = Math.Clamp(search?.PageSize ?? 20, 1, 100);

        if (search != null)
        {
            if (!string.IsNullOrWhiteSpace(search.Q))
            {
                var queryText = search.Q.Trim().ToLower();
                entity = entity.Where(x =>
                    x.Name.ToLower().Contains(queryText) ||
                    (x.Description != null && x.Description.ToLower().Contains(queryText)));
            }
            if (search.SalonId.HasValue)
            {
                entity = entity.Where(x => x.SalonId == search.SalonId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search.Name))
            {
                entity = entity.Where(x => x.Name.Contains(search.Name));
            }

            if (search.IsAvailable.HasValue)
            {
                entity = entity.Where(x => x.IsAvailable == search.IsAvailable.Value);
            }

            if (search.IsDeleted.HasValue)
            {
                entity = entity.Where(x => x.IsDeleted == search.IsDeleted.Value);
            }
        }

        var cacheQ = search?.Q?.Trim().ToLowerInvariant() ?? string.Empty;
        var name = search?.Name?.Trim().ToLowerInvariant() ?? string.Empty;
        var salonId = search?.SalonId?.ToString() ?? "null";
        var isAvailable = search?.IsAvailable?.ToString() ?? "null";
        var isDeleted = search?.IsDeleted?.ToString() ?? "null";
        var version = _cache.GetOrCreate(CacheVersionKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
            return 1;
        });
        var cacheKey = $"salon-amenities:get:v{version}:{page}:{pageSize}:{cacheQ}:{name}:{salonId}:{isAvailable}:{isDeleted}";

        if (_cache.TryGetValue(cacheKey, out List<SalonAmenityDto>? cached) && cached != null)
        {
            return cached;
        }

        var list = await entity
            .OrderBy(x => x.DisplayOrder)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var dtoList = _mapper.Map<List<SalonAmenityDto>>(list);
        _cache.Set(cacheKey, dtoList, CacheDuration);
        return dtoList;
    }

    public override async Task<SalonAmenityDto> Insert(SalonAmenityInsertRequest request)
    {
        var result = await base.Insert(request);
        BumpCacheVersion();
        return result;
    }

    public override async Task<SalonAmenityDto> Update(int id, SalonAmenityUpdateRequest request)
    {
        var result = await base.Update(id, request);
        BumpCacheVersion();
        return result;
    }

    public override async Task<SalonAmenityDto> Delete(int id)
    {
        var result = await base.Delete(id);
        BumpCacheVersion();
        return result;
    }

    private void BumpCacheVersion()
    {
        var currentVersion = _cache.Get<int?>(CacheVersionKey) ?? 1;
        _cache.Set(CacheVersionKey, currentVersion + 1, TimeSpan.FromDays(1));
    }
}
