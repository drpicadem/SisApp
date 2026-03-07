using ŠišAppApi.Models;

namespace ŠišAppApi.Services
{
    public interface INotificationService
    {
        Task<Notification> CreateNotification(int userId, string message, string type, string? data = null);
        Task<IEnumerable<Notification>> GetUserNotifications(int userId, bool unreadOnly = false);
        Task MarkAsRead(int notificationId, int userId);
        Task MarkAllAsRead(int userId);
    }
}
