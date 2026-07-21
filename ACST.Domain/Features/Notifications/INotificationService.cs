using ACST.Domain.DTOs.Notification;
using ACST.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ACST.Domain.Features.Notifications
{
    public interface INotificationService
    {
        Task<Result<List<NotificationResponse>>> GetUserNotificationsAsync(int userId, bool unreadOnly = false);
        Task<Result<NotificationCountResponse>> GetUnreadCountAsync(int userId);
        Task<Result> MarkAsReadAsync(int userId, long notificationId);
        Task<Result> MarkAllAsReadAsync(int userId);

        // Background Processor Methods
        Task ProcessUpcomingClassNotificationsAsync();
        Task ProcessPostClass30MinRemindersAsync();
        Task ProcessEveningAttendanceRemindersAsync();
        Task PurgeOldNotificationsAsync();
    }
}
