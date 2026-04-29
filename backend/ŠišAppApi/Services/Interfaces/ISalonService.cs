using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Models.Requests;
using ŠišAppApi.Models.SearchObjects;

namespace ŠišAppApi.Services.Interfaces
{
    public interface ISalonService : ICRUDService<SalonDto, SalonSearchObject, SalonInsertRequest, SalonUpdateRequest>
    {
        Task<SalonDto> ToggleStatusAsync(int id);
        Task<SalonDto> UpdateSalonImageAsync(int salonId, string imageId);
        Task<bool> CanBarberUpdateSalonAsync(int userId, int salonId);
    }
}
