using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Constants;
using ŠišAppApi.Services;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private const int DefaultPageSize = 20;
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService _currentUser;

        public NotificationsController(INotificationService notificationService, ICurrentUserService currentUser)
        {
            _notificationService = notificationService;
            _currentUser = currentUser;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> Get(
            int userId,
            [FromQuery] bool unreadOnly = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = DefaultPageSize)
        {
            var currentUserId = _currentUser.UserId;
            var role = _currentUser.Role;
            if (!currentUserId.HasValue || (currentUserId.Value != userId && role != AppRoles.Admin))
            {
                return Forbid();
            }

            var notifications = await _notificationService.GetUserNotifications(userId, unreadOnly, page, pageSize);
            return Ok(notifications);
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var currentUserId = _currentUser.UserId;
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            await _notificationService.MarkAsRead(id, currentUserId.Value);
            return NoContent();
        }

        [HttpPut("user/{userId}/read-all")]
        public async Task<IActionResult> MarkAllAsRead(int userId)
        {
            var currentUserId = _currentUser.UserId;
            var role = _currentUser.Role;
            if (!currentUserId.HasValue || (currentUserId.Value != userId && role != AppRoles.Admin))
            {
                return Forbid();
            }

            await _notificationService.MarkAllAsRead(userId);
            return NoContent();
        }
    }
}
