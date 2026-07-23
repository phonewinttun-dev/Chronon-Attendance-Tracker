using System;

namespace ACST.Domain.DTOs.Notification
{
    public class NotificationResponse
    {
        public long NotificationId { get; set; }
        public int UserId { get; set; }
        public long? SessionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string NotificationType { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime TriggeredAt { get; set; }
        public string? ModuleName { get; set; }
        public DateTime? SessionStartDatetime { get; set; }
        public DateTime? SessionEndDatetime { get; set; }
    }

    public class NotificationCountResponse
    {
        public int UnreadCount { get; set; }
    }
}
