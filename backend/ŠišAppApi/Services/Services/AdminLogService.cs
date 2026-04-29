using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Data;
using ŠišAppApi.Models;
using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Services.Services
{
    public class AdminLogService : IAdminLogService
    {
        private readonly ApplicationDbContext _context;

        public AdminLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(int adminUserId, string action, string entityType, int? entityId, string notes,
            string? ipAddress = null, string? userAgent = null)
        {
            var admin = await _context.Admins
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.UserId == adminUserId);

            if (admin == null)
                return;

            _context.AdminLogs.Add(new AdminLog
            {
                AdminId = admin.Id,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Notes = notes,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public async Task<PagedResult<AdminLogEntryDto>> GetLogsAsync(
            string? action, DateTime? from, DateTime? to, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var query = _context.AdminLogs
                .AsNoTracking()
                .Include(l => l.Admin)
                .ThenInclude(a => a.User)
                .Where(l => !l.IsDeleted);

            if (!string.IsNullOrWhiteSpace(action))
                query = query.Where(l => l.Action.Contains(action));

            if (from.HasValue)
                query = query.Where(l => l.CreatedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(l => l.CreatedAt <= to.Value);

            var totalCount = await query.CountAsync();

            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new AdminLogEntryDto
                {
                    Id = l.Id,
                    AdminId = l.AdminId,
                    AdminName = l.Admin != null && l.Admin.User != null
                        ? l.Admin.User.FirstName + " " + l.Admin.User.LastName
                        : "Nepoznato",
                    Action = l.Action,
                    EntityType = l.EntityType,
                    EntityId = l.EntityId,
                    Notes = l.Notes,
                    IpAddress = l.IpAddress,
                    UserAgent = l.UserAgent,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<AdminLogEntryDto>
            {
                Items = logs,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
    }
}
