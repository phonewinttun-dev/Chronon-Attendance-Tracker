using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.Features.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ACST.Domain.Tests.Features.Notifications;

public class NotificationServiceTests
{
    private readonly AppDbContext _context;
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);

        var configValues = new Dictionary<string, string?>
        {
            {"NotificationSettings:UpcomingClassLeadTimeMinutes", "30"}
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        _service = new NotificationService(_context, configuration);
    }

    [Fact]
    public async Task ProcessUpcomingClassNotificationsAsync_UpcomingClassWithin30Mins_CreatesNotificationOnce()
    {
        // Arrange
        var user = new TblUser { UserId = 1, FullName = "Test User", Email = "test@example.com" };
        var semester = new TblSemester { Id = 1, Name = "Sem-1", StartDate = DateOnly.FromDateTime(DateTime.Today), EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(3)), User = user, UserId = 1 };
        var module = new TblModule { Id = 1, Name = "Math 101", ModuleCode = "M101", User = user, UserId = 1 };
        var recSched = new TblRecurringSchedule { Id = 1, Module = module, Semester = semester, StartTime = new TimeOnly(10, 0, 0), EndTime = new TimeOnly(11, 0, 0), User = user, UserId = 1 };

        var now = DateTime.UtcNow;
        var upcomingSession = new TblSession
        {
            Id = 100,
            Module = module,
            Semester = semester,
            RecurringSchedule = recSched,
            SessionDate = DateOnly.FromDateTime(now),
            StartDatetime = now.AddMinutes(15), // Within 30 min window
            EndDatetime = now.AddMinutes(75),
            Status = "Not Marked",
            UserId = 1
        };

        _context.TblUsers.Add(user);
        _context.TblSemesters.Add(semester);
        _context.TblModules.Add(module);
        _context.TblRecurringSchedules.Add(recSched);
        _context.TblSessions.Add(upcomingSession);
        await _context.SaveChangesAsync();

        // Act - Run processor twice to test deduplication
        await _service.ProcessUpcomingClassNotificationsAsync();
        await _service.ProcessUpcomingClassNotificationsAsync();

        // Assert
        var notifications = await _context.TblNotifications.Where(n => n.UserId == 1).ToListAsync();
        Assert.Single(notifications);
        Assert.Equal("UpcomingClass", notifications[0].NotificationType);
        Assert.Equal(100, notifications[0].SessionId);
    }

    [Fact]
    public async Task ProcessPostClass30MinRemindersAsync_SessionEndedOver30MinsAgo_CreatesReminderOnce()
    {
        // Arrange
        var user = new TblUser { UserId = 1, FullName = "Test User", Email = "test@example.com" };
        var semester = new TblSemester { Id = 1, Name = "Sem-1", StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-10)), EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(3)), User = user, UserId = 1 };
        var module = new TblModule { Id = 1, Name = "Physics", ModuleCode = "P101", User = user, UserId = 1 };
        var recSched = new TblRecurringSchedule { Id = 1, Module = module, Semester = semester, StartTime = new TimeOnly(9, 0, 0), EndTime = new TimeOnly(10, 0, 0), User = user, UserId = 1 };

        var now = DateTime.UtcNow;
        var endedSession = new TblSession
        {
            Id = 200,
            Module = module,
            Semester = semester,
            RecurringSchedule = recSched,
            SessionDate = DateOnly.FromDateTime(now),
            StartDatetime = now.AddMinutes(-100),
            EndDatetime = now.AddMinutes(-40), // Ended 40 mins ago (> 30 min threshold)
            Status = "Not Marked",
            UserId = 1
        };

        _context.TblUsers.Add(user);
        _context.TblSemesters.Add(semester);
        _context.TblModules.Add(module);
        _context.TblRecurringSchedules.Add(recSched);
        _context.TblSessions.Add(endedSession);
        await _context.SaveChangesAsync();

        // Act - Run processor twice to test deduplication
        await _service.ProcessPostClass30MinRemindersAsync();
        await _service.ProcessPostClass30MinRemindersAsync();

        // Assert
        var notifications = await _context.TblNotifications.Where(n => n.UserId == 1).ToListAsync();
        Assert.Single(notifications);
        Assert.Equal("AttendanceReminder30Min", notifications[0].NotificationType);
    }

    [Fact]
    public async Task PurgeOldNotificationsAsync_RemovesNotificationsOlderThan7Days()
    {
        // Arrange
        var oldNotification = new TblNotification
        {
            UserId = 1,
            Title = "Old",
            Message = "Old message",
            NotificationType = "UpcomingClass",
            CreatedAt = DateTime.UtcNow.AddDays(-8) // 8 days old
        };

        var recentNotification = new TblNotification
        {
            UserId = 1,
            Title = "Recent",
            Message = "Recent message",
            NotificationType = "UpcomingClass",
            CreatedAt = DateTime.UtcNow.AddDays(-2) // 2 days old
        };

        _context.TblNotifications.AddRange(oldNotification, recentNotification);
        await _context.SaveChangesAsync();

        // Act
        await _service.PurgeOldNotificationsAsync();

        // Assert
        var remaining = await _context.TblNotifications.ToListAsync();
        Assert.Single(remaining);
        Assert.Equal("Recent", remaining[0].Title);
    }
}
