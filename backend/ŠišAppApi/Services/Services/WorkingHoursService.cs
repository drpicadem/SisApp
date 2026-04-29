using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Data;
using ŠišAppApi.Filters;
using ŠišAppApi.Models;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Services.Services
{
    public class WorkingHoursService : IWorkingHoursService
    {
        private readonly ApplicationDbContext _context;

        public WorkingHoursService(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<Barber> GetBarberByUserIdAsync(int userId)
        {
            var barber = await _context.Barbers.FirstOrDefaultAsync(b => b.UserId == userId);
            if (barber == null)
                throw new UserException("Niste registrirani kao frizer.");
            return barber;
        }

        public async Task<IEnumerable<WorkingHours>> GetMyScheduleAsync(int userId, int page, int pageSize)
        {
            var (p, ps) = Normalize(page, pageSize);
            var barber = await GetBarberByUserIdAsync(userId);

            return await _context.WorkingHours
                .Where(wh => wh.BarberId == barber.Id && !wh.IsDeleted)
                .OrderBy(wh => wh.DayOfWeek)
                .ThenBy(wh => wh.StartTime)
                .Skip((p - 1) * ps)
                .Take(ps)
                .ToListAsync();
        }

        public async Task<IEnumerable<WorkingHours>> GetBarberScheduleAsync(int barberId, int page, int pageSize)
        {
            var (p, ps) = Normalize(page, pageSize);

            return await _context.WorkingHours
                .Where(wh => wh.BarberId == barberId && !wh.IsDeleted && wh.IsWorking)
                .OrderBy(wh => wh.DayOfWeek)
                .ThenBy(wh => wh.StartTime)
                .Skip((p - 1) * ps)
                .Take(ps)
                .ToListAsync();
        }

        public async Task<WorkingHours> CreateWorkingHoursAsync(int userId, WorkingHoursUpsertRequest dto)
        {
            var barber = await GetBarberByUserIdAsync(userId);

            var existing = await _context.WorkingHours
                .FirstOrDefaultAsync(wh => wh.BarberId == barber.Id && wh.DayOfWeek == dto.DayOfWeek && !wh.IsDeleted);

            if (existing != null)
                throw new UserException("Već imate definirano radno vrijeme za taj dan. Uredite postojeće.");

            var workingHours = new WorkingHours
            {
                BarberId = barber.Id,
                DayOfWeek = dto.DayOfWeek,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                IsWorking = dto.IsWorking,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkingHours.Add(workingHours);
            await _context.SaveChangesAsync();
            return workingHours;
        }

        public async Task<WorkingHours> UpdateWorkingHoursAsync(int id, int userId, WorkingHoursUpsertRequest dto)
        {
            var barber = await GetBarberByUserIdAsync(userId);

            var workingHours = await _context.WorkingHours.FindAsync(id);
            if (workingHours == null)
                throw new UserException("Radno vrijeme nije pronađeno.");

            if (workingHours.BarberId != barber.Id)
                throw new UserException("Možete ažurirati samo svoje radno vrijeme.");

            workingHours.DayOfWeek = dto.DayOfWeek;
            workingHours.StartTime = dto.StartTime;
            workingHours.EndTime = dto.EndTime;
            workingHours.IsWorking = dto.IsWorking;
            workingHours.Notes = dto.Notes;
            workingHours.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return workingHours;
        }

        public async Task DeleteWorkingHoursAsync(int id, int userId)
        {
            var barber = await GetBarberByUserIdAsync(userId);

            var workingHours = await _context.WorkingHours.FindAsync(id);
            if (workingHours == null)
                throw new UserException("Radno vrijeme nije pronađeno.");

            if (workingHours.BarberId != barber.Id)
                throw new UserException("Možete obrisati samo svoje radno vrijeme.");

            workingHours.IsDeleted = true;
            workingHours.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        private static (int page, int pageSize) Normalize(int page, int pageSize)
        {
            var p = page < 1 ? 1 : page;
            var ps = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
            return (p, ps);
        }
    }
}
