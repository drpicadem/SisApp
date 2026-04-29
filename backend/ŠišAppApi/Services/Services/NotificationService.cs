using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Data;
using Microsoft.AspNetCore.SignalR;
using ŠišAppApi.Hubs;
using ŠišAppApi.Models;

namespace ŠišAppApi.Services
{
    public class NotificationService : INotificationService
    {
        private const int MaxPageSize = 100;
        private const int DefaultPageSize = 20;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(ApplicationDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<Notification> CreateNotification(int userId, string message, string type, string? data = null, string? title = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                Title = string.IsNullOrWhiteSpace(title) ? ResolveDefaultTitle(type) : title.Trim(),
                Type = type,
                Data = data,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.Group($"user-{userId}").SendAsync("notification_created", new
            {
                id = notification.Id,
                userId = notification.UserId,
                type = notification.Type,
                title = notification.Title,
                message = notification.Message,
                data = notification.Data,
                isRead = notification.IsRead,
                sentAt = notification.SentAt,
                readAt = notification.ReadAt
            });

            return notification;
        }

        private static string ResolveDefaultTitle(string type)
        {
            return type switch
            {
                "Payment" => "Plaćanje",
                "Cancellation" => "Otkazivanje",
                "AppointmentReminder" => "Podsjetnik",
                "Review" => "Recenzija",
                _ => "Obavještenje"
            };
        }

        public async Task<IEnumerable<Notification>> GetUserNotifications(int userId, bool unreadOnly = false, int page = 1, int pageSize = DefaultPageSize)
        {
            var normalizedPage = page < 1 ? 1 : page;
            var normalizedPageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
            var skip = (normalizedPage - 1) * normalizedPageSize;

            var query = _context.Notifications
                .Where(n => n.UserId == userId && !n.IsDeleted);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            return await query
                .OrderByDescending(n => n.SentAt)
                .Skip(skip)
                .Take(normalizedPageSize)
                .ToListAsync();
        }

        public async Task MarkAsRead(int notificationId, int userId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null && !notification.IsRead && notification.UserId == userId)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await _hubContext.Clients.Group($"user-{userId}").SendAsync("notification_read", new
                {
                    id = notificationId,
                    readAt = notification.ReadAt
                });
            }
        }

        public async Task MarkAllAsRead(int userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (notifications.Any())
            {
                var now = DateTime.UtcNow;
                foreach (var n in notifications)
                {
                    n.IsRead = true;
                    n.ReadAt = now;
                }
                await _context.SaveChangesAsync();
                await _hubContext.Clients.Group($"user-{userId}").SendAsync("notification_read_all", new
                {
                    userId,
                    readAt = now
                });
            }
        }
    }
}
