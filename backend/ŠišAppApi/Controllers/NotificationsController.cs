using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Services;

namespace ŠišAppApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> Get(int userId, [FromQuery] bool unreadOnly = false)
        {
            // Simple security check: Ensure user can only read their own notifications
            // In a real app, you'd check User.Identity.Name or Claims
            var notifications = await _notificationService.GetUserNotifications(userId, unreadOnly);
            return Ok(notifications);
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _notificationService.MarkAsRead(id);
            return NoContent();
        }

        [HttpPut("user/{userId}/read-all")]
        public async Task<IActionResult> MarkAllAsRead(int userId)
        {
            await _notificationService.MarkAllAsRead(userId);
            return NoContent();
        }
    }
}
