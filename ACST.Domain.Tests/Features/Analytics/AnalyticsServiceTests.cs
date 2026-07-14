using System;
using System.Linq;
using System.Threading.Tasks;
using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.Analytics;
using ACST.Domain.Features.Analytics;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ACST.Domain.Tests.Features.Analytics;

public class AnalyticsServiceTests
{
    private readonly AppDbContext _context;
    private readonly AnalyticsService _service;

    public AnalyticsServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        _context = new AppDbContext(options);
        _service = new AnalyticsService(_context);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldCalculateReconciliationAndBreakdownsCorrectly()
    {
        // Arrange
        var semester = new TblSemester
        {
            Id = 1,
            Name = "Test Semester",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31)
        };
        _context.TblSemesters.Add(semester);

        var module = new TblModule { Id = 1, Name = "Module 1", ModuleCode = "MOD-1", SemesterId = 1 };
        _context.TblModules.Add(module);

        var schedule = new TblRecurringSchedule { Id = 1, ModuleId = 1, SemesterId = 1 };
        _context.TblRecurringSchedules.Add(schedule);

        // Add 5 sessions:
        // 1. Jan 5 (Monday) - Present
        // 2. Jan 12 (Monday) - Absent
        // 3. Jan 19 (Monday) - Late
        // 4. Jan 26 (Monday) - Cancelled (excluded)
        // 5. Jan 15 (Thursday) - Holiday (excluded)
        var sessions = new[]
        {
            new TblSession
            {
                Id = 1, SemesterId = 1, ModuleId = 1, RecurringScheduleId = 1,
                SessionDate = new DateOnly(2026, 1, 5), StartDatetime = new DateTime(2026, 1, 5, 9, 0, 0), EndDatetime = new DateTime(2026, 1, 5, 10, 30, 0),
                Status = "Present"
            },
            new TblSession
            {
                Id = 2, SemesterId = 1, ModuleId = 1, RecurringScheduleId = 1,
                SessionDate = new DateOnly(2026, 1, 12), StartDatetime = new DateTime(2026, 1, 12, 9, 0, 0), EndDatetime = new DateTime(2026, 1, 12, 10, 30, 0),
                Status = "Absent"
            },
            new TblSession
            {
                Id = 3, SemesterId = 1, ModuleId = 1, RecurringScheduleId = 1,
                SessionDate = new DateOnly(2026, 1, 19), StartDatetime = new DateTime(2026, 1, 19, 9, 0, 0), EndDatetime = new DateTime(2026, 1, 19, 10, 30, 0),
                Status = "Late"
            },
            new TblSession
            {
                Id = 4, SemesterId = 1, ModuleId = 1, RecurringScheduleId = 1,
                SessionDate = new DateOnly(2026, 1, 26), StartDatetime = new DateTime(2026, 1, 26, 9, 0, 0), EndDatetime = new DateTime(2026, 1, 26, 10, 30, 0),
                Status = "Cancelled"
            },
            new TblSession
            {
                Id = 5, SemesterId = 1, ModuleId = 1, RecurringScheduleId = 1,
                SessionDate = new DateOnly(2026, 1, 15), StartDatetime = new DateTime(2026, 1, 15, 9, 0, 0), EndDatetime = new DateTime(2026, 1, 15, 10, 30, 0),
                Status = "Holiday"
            }
        };

        _context.TblSessions.AddRange(sessions);
        await _context.SaveChangesAsync();

        // Verify cache miss fallback works first and creates the entry
        var initialResult = await _service.GetDashboardSummaryAsync(1);
        Assert.True(initialResult.IsSuccess);
        Assert.Equal("Test Semester", initialResult.Data.SemesterName);

        // Verify precalculated entry exists in database
        var cachedRecord = await _context.TblSemesterDashboardSummaries.FirstOrDefaultAsync(c => c.SemesterId == 1);
        Assert.NotNull(cachedRecord);
        Assert.Equal("Test Semester", cachedRecord.SemesterName);

        // Manually mutate the cached record to prove subsequent reads serve from cache
        cachedRecord.SemesterName = "Cached Test Semester";
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDashboardSummaryAsync(1);
        var dailyWeeklyResult = await _service.GetDashboardDailyWeeklyAsync(1);
        var modulesResult = await _service.GetDashboardModulesAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        var data = result.Data;
        Assert.NotNull(data);

        Assert.Equal("Cached Test Semester", data.SemesterName);
        Assert.Equal(new DateOnly(2026, 1, 1), data.StartDate);
        Assert.Equal(new DateOnly(2026, 1, 31), data.EndDate);

        // Reconciliation
        Assert.Equal(5, data.TotalSessions);
        Assert.Equal(1, data.PresentSessions);
        Assert.Equal(1, data.AbsentSessions);
        Assert.Equal(0, data.LateSessions);
        Assert.Equal(1, data.CancelledSessions);
        Assert.Equal(1, data.HolidaySessions);
        Assert.Equal(1, data.NotMarkedSessions);
        Assert.Equal(3, data.ValidSessions); // 5 - (1 cancelled + 1 holiday)
        
        // Present (1) / Valid (3) * 100 = 33.33%
        Assert.Equal(33.33, data.SemesterHealthRate);
        Assert.Equal(33.33, data.CalculatedRate);
        Assert.Null(data.TodayAttendanceRate); // No sessions scheduled for today in this test

        // Verify that breakdowns are empty in summary DTO
        Assert.Empty(data.DailyAttendance);
        Assert.Empty(data.WeeklyAttendance);
        Assert.Empty(data.MonthlyAttendance);
        Assert.Empty(data.ModuleAttendance);

        // Assert DailyWeekly breakdown
        Assert.True(dailyWeeklyResult.IsSuccess);
        var dwData = dailyWeeklyResult.Data;
        Assert.NotNull(dwData);

        // Daily breakdown
        // Mondays: 4 sessions (Jan 5 - Present, Jan 12 - Absent, Jan 19 - Late, Jan 26 - Cancelled)
        // Valid Monday sessions = 3 (Total 4 - Cancelled 1)
        // Present Monday sessions = 1
        // Monday Rate = 1/3 * 100 = 33.33%
        var mondayStats = dwData.DailyAttendance.FirstOrDefault(d => d.DayOfWeek == "Monday");
        Assert.NotNull(mondayStats);
        Assert.Equal(4, mondayStats.TotalSessions);
        Assert.Equal(1, mondayStats.Present);
        Assert.Equal(1, mondayStats.Absent);
        Assert.Equal(0, mondayStats.Late);
        Assert.Equal(1, mondayStats.Cancelled);
        Assert.Equal(3, mondayStats.ValidSessions);
        Assert.Equal(33.33, mondayStats.AttendanceRate);

        // Thursdays: 1 session (Jan 15 - Holiday)
        // Valid Thursday sessions = 0 (Total 1 - Holiday 1)
        // Thursday Rate = 0%
        var thursdayStats = dwData.DailyAttendance.FirstOrDefault(d => d.DayOfWeek == "Thursday");
        Assert.NotNull(thursdayStats);
        Assert.Equal(1, thursdayStats.TotalSessions);
        Assert.Equal(0, thursdayStats.Present);
        Assert.Equal(1, thursdayStats.Holiday);
        Assert.Equal(0, thursdayStats.ValidSessions);
        Assert.Equal(0, thursdayStats.AttendanceRate);

        // Weekly breakdown (calendar weeks starting Monday)
        Assert.Equal(4, dwData.WeeklyAttendance.Count);

        // Week 1 (Jan 5 - Jan 11): Jan 5 session (Present) -> 100%
        var w1 = dwData.WeeklyAttendance.FirstOrDefault(w => w.WeekStartDate == new DateOnly(2026, 1, 5));
        Assert.NotNull(w1);
        Assert.Equal(1, w1.TotalSessions);
        Assert.Equal(1, w1.Present);
        Assert.Equal(1, w1.ValidSessions);
        Assert.Equal(100.0, w1.AttendanceRate);

        // Week 2 (Jan 12 - Jan 18): Jan 12 (Absent), Jan 15 (Holiday) -> Valid = 1, Present = 0 -> 0%
        var w2 = dwData.WeeklyAttendance.FirstOrDefault(w => w.WeekStartDate == new DateOnly(2026, 1, 12));
        Assert.NotNull(w2);
        Assert.Equal(2, w2.TotalSessions);
        Assert.Equal(0, w2.Present);
        Assert.Equal(1, w2.Holiday);
        Assert.Equal(1, w2.ValidSessions);
        Assert.Equal(0.0, w2.AttendanceRate);

        // Monthly breakdown
        Assert.Single(dwData.MonthlyAttendance);
        var m1 = dwData.MonthlyAttendance.First();
        Assert.Equal("January 2026", m1.MonthName);
        Assert.Equal(5, m1.TotalSessions);
        Assert.Equal(3, m1.ValidSessions);
        Assert.Equal(33.33, m1.AttendanceRate);

        // Assert Modules breakdown
        Assert.True(modulesResult.IsSuccess);
        var modulesData = modulesResult.Data;
        Assert.NotNull(modulesData);
        Assert.Single(modulesData);
        var mod1 = modulesData.First();
        Assert.Equal(1, mod1.ModuleId);
        Assert.Equal("Module 1", mod1.ModuleName);
        Assert.Equal(5, mod1.TotalSessions);
        Assert.Equal(1, mod1.TotalPresent);
        Assert.Equal(1, mod1.TotalAbsent);
        Assert.Equal(1, mod1.Cancelled);
        Assert.Equal(1, mod1.Holiday);
        Assert.Equal(0, mod1.NotMarked);
        Assert.Equal(33.33, mod1.AttendanceRate);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldCalculateTodayAttendanceRateCorrectly()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(6.5));
        var semester = new TblSemester
        {
            Id = 2,
            Name = "Semester 2",
            StartDate = today.AddDays(-10),
            EndDate = today.AddDays(10)
        };
        _context.TblSemesters.Add(semester);

        var module = new TblModule { Id = 2, Name = "Module 2", ModuleCode = "MOD-2", SemesterId = 2 };
        _context.TblModules.Add(module);

        var schedule = new TblRecurringSchedule { Id = 2, ModuleId = 2, SemesterId = 2 };
        _context.TblRecurringSchedules.Add(schedule);

        // Add sessions for today:
        // 1. Session today - Present (valid)
        // 2. Session today - Absent (valid)
        // 3. Session today - Cancelled (excluded)
        var sessions = new[]
        {
            new TblSession
            {
                Id = 10, SemesterId = 2, ModuleId = 2, RecurringScheduleId = 2,
                SessionDate = today, StartDatetime = DateTime.UtcNow, EndDatetime = DateTime.UtcNow.AddHours(1),
                Status = "Present"
            },
            new TblSession
            {
                Id = 11, SemesterId = 2, ModuleId = 2, RecurringScheduleId = 2,
                SessionDate = today, StartDatetime = DateTime.UtcNow.AddHours(2), EndDatetime = DateTime.UtcNow.AddHours(3),
                Status = "Absent"
            },
            new TblSession
            {
                Id = 12, SemesterId = 2, ModuleId = 2, RecurringScheduleId = 2,
                SessionDate = today, StartDatetime = DateTime.UtcNow.AddHours(4), EndDatetime = DateTime.UtcNow.AddHours(5),
                Status = "Cancelled"
            }
        };

        _context.TblSessions.AddRange(sessions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDashboardSummaryAsync(2);

        // Assert
        Assert.True(result.IsSuccess);
        var data = result.Data;
        Assert.NotNull(data);
        
        // Present (1) / Valid (2) * 100 = 50%
        Assert.Equal(50.0, data.TodayAttendanceRate);
    }
}
