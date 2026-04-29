using ŠišAppApi.Models;
using ŠišAppApi.Models.DTOs;

namespace ŠišAppApi.Services.Interfaces
{
    public interface ICityService
    {
        Task<PagedResult<CityItemDto>> GetCitiesAsync(string? q, int page, int pageSize);
    }
}
