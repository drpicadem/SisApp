using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Data;
using ŠišAppApi.Models;

namespace ŠišAppApi.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Notification> CreateNotification(int userId, string message, string type, string? data = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                Type = type,
                Data = data,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return notification;
        }

        public async Task<IEnumerable<Notification>> GetUserNotifications(int userId, bool unreadOnly = false)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId && !n.IsDeleted);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            return await query.OrderByDescending(n => n.SentAt).ToListAsync();
        }

        public async Task MarkAsRead(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
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
            }
        }
    }
}
