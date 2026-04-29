using ŠišAppApi.Models;
using ŠišAppApi.Models.DTOs;

namespace ŠišAppApi.Services.Interfaces
{
    public interface IAdminLogService
    {
        Task LogAsync(int adminUserId, string action, string entityType, int? entityId, string notes,
            string? ipAddress = null, string? userAgent = null);

        Task<PagedResult<AdminLogEntryDto>> GetLogsAsync(
            string? action, DateTime? from, DateTime? to, int page, int pageSize);
    }
}
