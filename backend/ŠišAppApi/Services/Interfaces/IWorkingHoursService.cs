using ŠišAppApi.Models;

namespace ŠišAppApi.Services.Interfaces
{
    public interface IWorkingHoursService
    {
        Task<IEnumerable<WorkingHours>> GetMyScheduleAsync(int userId, int page, int pageSize);
        Task<IEnumerable<WorkingHours>> GetBarberScheduleAsync(int barberId, int page, int pageSize);
        Task<WorkingHours> CreateWorkingHoursAsync(int userId, WorkingHoursUpsertRequest dto);
        Task<WorkingHours> UpdateWorkingHoursAsync(int id, int userId, WorkingHoursUpsertRequest dto);
        Task DeleteWorkingHoursAsync(int id, int userId);
    }

    public class WorkingHoursUpsertRequest
    {
        public int DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsWorking { get; set; } = true;
        public string? Notes { get; set; }
    }
}
