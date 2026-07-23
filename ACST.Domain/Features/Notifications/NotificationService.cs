using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.Notification;
using ACST.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ACST.Domain.Features.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public NotificationService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<Result<List<NotificationResponse>>> GetUserNotificationsAsync(int userId, bool unreadOnly = false)
        {
            try
            {
                var query = _context.TblNotifications
                    .Include(n => n.Session)
                    .ThenInclude(s => s!.Module)
                    .Where(n => n.UserId == userId);

                if (unreadOnly)
                {
                    query = query.Where(n => !n.IsRead);
                }

                var list = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new NotificationResponse
                    {
                        NotificationId = n.NotificationId,
                        UserId = n.UserId,
                        SessionId = n.SessionId,
                        Title = n.Title,
                        Message = n.Message,
                        NotificationType = n.NotificationType,
                        IsRead = n.IsRead,
                        CreatedAt = n.CreatedAt,
                        TriggeredAt = n.TriggeredAt,
                        ModuleName = n.Session != null && n.Session.Module != null ? n.Session.Module.Name : null,
                        SessionStartDatetime = n.Session != null ? n.Session.StartDatetime : null,
                        SessionEndDatetime = n.Session != null ? n.Session.EndDatetime : null
                    })
                    .ToListAsync();

                return Result<List<NotificationResponse>>.Success(list);
            }
            catch (Exception ex)
            {
                return Result<List<NotificationResponse>>.Failure($"Error retrieving notifications: {ex.Message}");
            }
        }

        public async Task<Result<NotificationCountResponse>> GetUnreadCountAsync(int userId)
        {
            try
            {
                var count = await _context.TblNotifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);

                return Result<NotificationCountResponse>.Success(new NotificationCountResponse { UnreadCount = count });
            }
            catch (Exception ex)
            {
                return Result<NotificationCountResponse>.Failure($"Error retrieving unread count: {ex.Message}");
            }
        }

        public async Task<Result> MarkAsReadAsync(int userId, long notificationId)
        {
            try
            {
                var notification = await _context.TblNotifications
                    .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

                if (notification == null)
                {
                    return Result.Failure("Notification not found.");
                }

                notification.IsRead = true;
                await _context.SaveChangesAsync();

                return Result.Success("Notification marked as read.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error updating notification: {ex.Message}");
            }
        }

        public async Task<Result> MarkAllAsReadAsync(int userId)
        {
            try
            {
                var unreadNotifications = await _context.TblNotifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                foreach (var n in unreadNotifications)
                {
                    n.IsRead = true;
                }

                await _context.SaveChangesAsync();
                return Result.Success("All notifications marked as read.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error marking all notifications as read: {ex.Message}");
            }
        }

        public async Task ProcessUpcomingClassNotificationsAsync()
        {
            var leadTimeMinutes = int.Parse(_configuration["NotificationSettings:UpcomingClassLeadTimeMinutes"] ?? "30");
            var now = DateTime.UtcNow;
            var targetStart = now.AddMinutes(leadTimeMinutes);

            var upcomingSessions = await _context.TblSessions
                .Include(s => s.Module)
                .Include(s => s.Semester)
                .Where(s => !s.IsDeleted &&
                            s.Status != "Holiday" && s.Status != "Cancelled" &&
                            s.StartDatetime >= now && s.StartDatetime <= targetStart)
                .ToListAsync();

            if (!upcomingSessions.Any()) return;

            var sessionIds = upcomingSessions.Select(s => s.Id).ToList();
            var existingNotificationSessionIds = await _context.TblNotifications
                .Where(n => n.NotificationType == "UpcomingClass" && n.SessionId.HasValue && sessionIds.Contains(n.SessionId.Value))
                .Select(n => n.SessionId!.Value)
                .ToListAsync();

            var newNotifications = new List<TblNotification>();

            foreach (var session in upcomingSessions)
            {
                if (existingNotificationSessionIds.Contains(session.Id)) continue;

                var targetUserId = session.UserId ?? session.Semester?.UserId ?? 0;
                if (targetUserId == 0) continue;

                newNotifications.Add(new TblNotification
                {
                    UserId = targetUserId,
                    SessionId = session.Id,
                    Title = $"Upcoming Class: {session.Module?.Name ?? "Class"}",
                    Message = $"Your class '{session.Module?.Name}' is scheduled to start in {leadTimeMinutes} minutes at {session.StartDatetime.ToLocalTime():HH:mm}.",
                    NotificationType = "UpcomingClass",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    TriggeredAt = DateTime.UtcNow
                });
            }

            if (newNotifications.Any())
            {
                _context.TblNotifications.AddRange(newNotifications);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ProcessPostClass30MinRemindersAsync()
        {
            var now = DateTime.UtcNow;
            var threshold = now.AddMinutes(-30);
            var recentLimit = DateOnly.FromDateTime(now.AddDays(-2));

            var unmarkedSessions = await _context.TblSessions
                .Include(s => s.Module)
                .Include(s => s.Semester)
                .Where(s => !s.IsDeleted &&
                            s.Status == "Not Marked" &&
                            s.EndDatetime <= threshold &&
                            s.SessionDate >= recentLimit)
                .ToListAsync();

            if (!unmarkedSessions.Any()) return;

            var sessionIds = unmarkedSessions.Select(s => s.Id).ToList();
            var existingReminderSessionIds = await _context.TblNotifications
                .Where(n => n.NotificationType == "AttendanceReminder30Min" && n.SessionId.HasValue && sessionIds.Contains(n.SessionId.Value))
                .Select(n => n.SessionId!.Value)
                .ToListAsync();

            var newNotifications = new List<TblNotification>();

            foreach (var session in unmarkedSessions)
            {
                if (existingReminderSessionIds.Contains(session.Id)) continue;

                var targetUserId = session.UserId ?? session.Semester?.UserId ?? 0;
                if (targetUserId == 0) continue;

                newNotifications.Add(new TblNotification
                {
                    UserId = targetUserId,
                    SessionId = session.Id,
                    Title = $"Attendance Reminder: {session.Module?.Name ?? "Class"}",
                    Message = $"It's been 30 minutes since '{session.Module?.Name}' ended. Please record your attendance.",
                    NotificationType = "AttendanceReminder30Min",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    TriggeredAt = DateTime.UtcNow
                });
            }

            if (newNotifications.Any())
            {
                _context.TblNotifications.AddRange(newNotifications);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ProcessEveningAttendanceRemindersAsync()
        {
            var now = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(DateTime.Today);

            var unmarkedSessionsToday = await _context.TblSessions
                .Include(s => s.Module)
                .Include(s => s.Semester)
                .Where(s => !s.IsDeleted &&
                            s.SessionDate == today &&
                            s.Status == "Not Marked")
                .ToListAsync();

            if (!unmarkedSessionsToday.Any()) return;

            var sessionIds = unmarkedSessionsToday.Select(s => s.Id).ToList();
            var existingEveningReminderSessionIds = await _context.TblNotifications
                .Where(n => n.NotificationType == "EveningAttendanceReminder" && n.SessionId.HasValue && sessionIds.Contains(n.SessionId.Value))
                .Select(n => n.SessionId!.Value)
                .ToListAsync();

            var newNotifications = new List<TblNotification>();

            foreach (var session in unmarkedSessionsToday)
            {
                if (existingEveningReminderSessionIds.Contains(session.Id)) continue;

                var targetUserId = session.UserId ?? session.Semester?.UserId ?? 0;
                if (targetUserId == 0) continue;

                newNotifications.Add(new TblNotification
                {
                    UserId = targetUserId,
                    SessionId = session.Id,
                    Title = "Evening Attendance Reminder",
                    Message = $"Reminder: You have not marked attendance for '{session.Module?.Name}' today.",
                    NotificationType = "EveningAttendanceReminder",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    TriggeredAt = DateTime.UtcNow
                });
            }

            if (newNotifications.Any())
            {
                _context.TblNotifications.AddRange(newNotifications);
                await _context.SaveChangesAsync();
            }
        }

        public async Task PurgeOldNotificationsAsync()
        {
            var cutoff = DateTime.UtcNow.AddDays(-7);
            var oldNotifications = await _context.TblNotifications
                .Where(n => n.CreatedAt < cutoff)
                .ToListAsync();

            if (oldNotifications.Any())
            {
                _context.TblNotifications.RemoveRange(oldNotifications);
                await _context.SaveChangesAsync();
            }
        }
    }
}
