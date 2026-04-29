using ŠišAppApi.Models.DTOs;

namespace ŠišAppApi.Services.Interfaces
{
    public interface IBarberService
    {
        Task<BarberProfileDto?> GetMyProfileAsync(int userId);
        Task<IEnumerable<BarberProfileDto>> GetBarbersBySalonAsync(int salonId, int page, int pageSize, int? serviceId);
        Task<BarberProfileDto> CreateBarberAsync(CreateBarberRequest dto);
        Task UpdateBarberImageAsync(int barberId, string imageId);
        Task<IEnumerable<BarberServiceItemDto>> GetBarberServicesAsync(int barberId, int page, int pageSize);
        Task AssignBarberServicesAsync(int barberId, List<int> serviceIds);
        Task RemoveBarberServiceAsync(int barberId, int serviceId);
        Task<BarberProfileDto> UpdateBarberAsync(int id, UpdateBarberRequest dto, int? adminUserId,
            string? ipAddress = null, string? userAgent = null);
    }

    public class CreateBarberRequest
    {
        public int SalonId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Bio { get; set; }
    }

    public class UpdateBarberRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Password { get; set; }
        public string? Bio { get; set; }
    }
}
