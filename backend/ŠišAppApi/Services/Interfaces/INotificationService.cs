using ŠišAppApi.Models;

namespace ŠišAppApi.Services
{
    public interface INotificationService
    {
        Task<Notification> CreateNotification(int userId, string message, string type, string? data = null, string? title = null);
        Task<IEnumerable<Notification>> GetUserNotifications(int userId, bool unreadOnly = false, int page = 1, int pageSize = 20);
        Task MarkAsRead(int notificationId, int userId);
        Task MarkAllAsRead(int userId);
    }
}
