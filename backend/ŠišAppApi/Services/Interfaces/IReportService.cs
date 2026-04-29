using ŠišAppApi.Models.DTOs;

namespace ŠišAppApi.Services.Interfaces
{
    public interface IReportService
    {
        Task<StatsDto> GetStatsAsync();
        Task<byte[]> GenerateStatsPdfAsync();
        Task<byte[]> GenerateAppointmentsPdfAsync();
        Task<byte[]> GenerateRevenuePdfAsync();
    }
}
