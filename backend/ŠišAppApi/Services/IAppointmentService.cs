using ŠišAppApi.Models; 
using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Models.Requests; 
using ŠišAppApi.Models.SearchObjects; 

namespace ŠišAppApi.Services
{
    public interface IAppointmentService : ICRUDService<AppointmentDto, AppointmentSearchObject, AppointmentInsertRequest, AppointmentUpdateRequest>
    {
        Task<IEnumerable<string>> GetAvailableSlots(int barberId, DateOnly date, int? serviceId = null);
        Task<AppointmentDto> Insert(AppointmentInsertRequest request, int userId); 
        Task<AppointmentDto> Cancel(int id, int userId); 
    }
}
