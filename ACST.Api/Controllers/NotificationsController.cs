using ACST.Domain.Features.Notifications;
using ACST.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ACST.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] int? userId = null, [FromQuery] bool unreadOnly = false)
        {
            var targetUserId = userId ?? GetCurrentUserId();
            if (targetUserId == 0)
            {
                return BadRequest(Result.Failure("User ID is required."));
            }

            var result = await _notificationService.GetUserNotificationsAsync(targetUserId, unreadOnly);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount([FromQuery] int? userId = null)
        {
            var targetUserId = userId ?? GetCurrentUserId();
            if (targetUserId == 0)
            {
                return BadRequest(Result.Failure("User ID is required."));
            }

            var result = await _notificationService.GetUnreadCountAsync(targetUserId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(long id, [FromQuery] int? userId = null)
        {
            var targetUserId = userId ?? GetCurrentUserId();
            if (targetUserId == 0)
            {
                return BadRequest(Result.Failure("User ID is required."));
            }

            var result = await _notificationService.MarkAsReadAsync(targetUserId, id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead([FromQuery] int? userId = null)
        {
            var targetUserId = userId ?? GetCurrentUserId();
            if (targetUserId == 0)
            {
                return BadRequest(Result.Failure("User ID is required."));
            }

            var result = await _notificationService.MarkAllAsReadAsync(targetUserId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var userId) ? userId : 0;
        }
    }
}
