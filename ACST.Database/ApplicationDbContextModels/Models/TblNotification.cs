using System;

namespace ACST.Database.ApplicationDbContextModels.Models;

public partial class TblNotification
{
    public long NotificationId { get; set; }

    public int UserId { get; set; }

    public long? SessionId { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string NotificationType { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime TriggeredAt { get; set; }

    public virtual TblUser User { get; set; } = null!;

    public virtual TblSession? Session { get; set; }
}
