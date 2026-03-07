using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Models.Requests;
using ŠišAppApi.Models.SearchObjects;

namespace ŠišAppApi.Services.Interfaces
{
    public interface IServiceService : ICRUDService<ServiceDto, ServiceSearchObject, ServiceInsertRequest, ServiceUpdateRequest>
    {
        Task<ServiceDto> DeleteAsBarber(int serviceId, int userId);
    }
}
