using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.ClassSession;
using ACST.Domain.DTOs.Module;
using ACST.Domain.Features.GoogleCalendar;
using ACST.Domain.Features.Analytics;
using ACST.Shared;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ACST.Domain.Features.ClassSessions;

public class ClassSessionService : IClassSessionService
{
    private readonly AppDbContext _context;
    private readonly IGoogleCalendarService _googleCalendarService;
    private readonly IConfiguration? _configuration;
    private readonly IBackgroundJobClient? _backgroundJobClient;
    private readonly IServiceProvider? _serviceProvider;
    private static readonly TimeSpan MyanmarOffset = TimeSpan.FromHours(6.5);

    public ClassSessionService(
        AppDbContext context, 
        IGoogleCalendarService googleCalendarService,
        IConfiguration? configuration = null,
        IBackgroundJobClient? backgroundJobClient = null,
        IServiceProvider? serviceProvider = null)
    {
        _context = context;
        _googleCalendarService = googleCalendarService;
        _configuration = configuration;
        _backgroundJobClient = backgroundJobClient;
        _serviceProvider = serviceProvider;
    }

    private IQueryable<TblSession> activeSession => _context.TblSessions
        .Include(s => s.Module)
        .Include(s => s.Semester)
        .AsNoTracking()
        .Where(s => !s.IsDeleted && (s.Module == null || !s.Module.IsDeleted) && (s.Semester == null || !s.Semester.IsDeleted));

    #region Get All Sessions
    public async Task<PagedResult<ClassSessionDto>> GetSessionsAsync(GetClassSessionsRequest request)
    {
        if (request is null)
        {
            return PagedResult<ClassSessionDto>.Failure("Request cannot be null.");
        }
        try
        {
            var query = activeSession;
            if (request.SemesterId.HasValue) query = query.Where(s => s.SemesterId == request.SemesterId.Value);
            if (request.ModuleId.HasValue) query = query.Where(s => s.ModuleId == request.ModuleId.Value);
            if (request.StartDate.HasValue) query = query.Where(s => s.SessionDate >= request.StartDate.Value);
            if (request.EndDate.HasValue) query = query.Where(s => s.SessionDate <= request.EndDate.Value);
            if (!string.IsNullOrEmpty(request.Status)) query = query.Where(s => s.Status == request.Status);
            if (request.DayOfWeek.HasValue) query = query.Where(s => (int)s.SessionDate.DayOfWeek == request.DayOfWeek.Value);

            int totalCount = await query.CountAsync();
            if (totalCount == 0)
            {
                return PagedResult<ClassSessionDto>.Success(new List<ClassSessionDto>(), new Pagination(request.PageNumber, request.PageSize, 0));
            }

            var orderedQuery = query.OrderBy(s => s.StartDatetime);

            var items = await orderedQuery
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(s => new ClassSessionDto
                    {
                        Id = s.Id,
                        RecurringScheduleId = s.RecurringScheduleId,
                        ModuleId = s.ModuleId,
                        ModuleName = s.Module.Name,
                        ModuleCode = s.Module.ModuleCode,
                        SemesterId = s.SemesterId,
                        SemesterName = s.Semester.Name,
                        SessionDate = s.SessionDate,
                        StartDatetime = s.StartDatetime,
                        EndDatetime = s.EndDatetime,
                        Status = s.Status,
                        MagicLinkToken = s.MagicLinkToken,
                        GoogleEventId = s.GoogleEventId
                    })
                    .ToListAsync();

            var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);

            return PagedResult<ClassSessionDto>.Success(items, pagination);
        }
        catch (Exception ex)
        {
            return PagedResult<ClassSessionDto>.Failure($"Failed to retrieve sessions: {ex.Message}");
        }
    }
    #endregion

    #region Get Session By Id
    public async Task<Result<ClassSessionDto>> GetSessionByIdAsync(long id)
    {
        try
        {
            var s = await activeSession.FirstOrDefaultAsync(s => s.Id == id);

            if (s == null) return Result<ClassSessionDto>.Failure("Session not found.");

            return Result<ClassSessionDto>.Success(new ClassSessionDto
            {
                Id = s.Id,
                RecurringScheduleId = s.RecurringScheduleId,
                ModuleId = s.ModuleId,
                ModuleName = s.Module.Name,
                ModuleCode = s.Module.ModuleCode,
                SemesterId = s.SemesterId,
                SemesterName = s.Semester.Name,
                SessionDate = s.SessionDate,
                StartDatetime = s.StartDatetime,
                EndDatetime = s.EndDatetime,
                Status = s.Status,
                MagicLinkToken = s.MagicLinkToken,
                GoogleEventId = s.GoogleEventId
            });
        }
        catch (Exception ex)
        {
            return Result<ClassSessionDto>.Failure($"Error getting session: {ex.Message}");
        }
    }
    #endregion

    #region Generate Sessions
    public async Task<Result> GenerateSessionsAsync(GenerateSessionsRequest request)
    {
        try
        {
            // Verify that the semester exists
            var semester = await _context.TblSemesters.FirstOrDefaultAsync(s => s.Id == request.SemesterId && !s.IsDeleted);
            if (semester is null)
            {
                return Result.Failure("Semester not found.");
            }

            // Query active recurring schedules with their modules
            var schedulesQuery = _context.TblRecurringSchedules
                .Include(r => r.Module)
                .Where(r => r.SemesterId == request.SemesterId && r.IsActive && !r.IsDeleted && (r.Module == null || !r.Module.IsDeleted));

            if (request.ModuleId.HasValue)
            {
                schedulesQuery = schedulesQuery.Where(r => r.ModuleId == request.ModuleId.Value);
            }

            var schedules = await schedulesQuery.ToListAsync();
            if (!schedules.Any()) return Result.Failure("No active recurring schedules found for this semester.");

            // Load active holidays and map to a HashSet for O(1) lookups
            var holidays = await _context.TblHolidays
                .Where(h => !h.IsDeleted && h.HolidayDate >= semester.StartDate && h.HolidayDate <= semester.EndDate)
                .Select(h => h.HolidayDate)
                .ToListAsync();

            var holidaySet = new HashSet<DateOnly>(holidays);

            // Fetch existing sessions and map to a HashSet of composite keys for O(1) lookups
            var existingSessionsQuery = await _context.TblSessions
                .Where(s => s.SemesterId == semester.Id && !s.IsDeleted)
                .Select(s => new { s.RecurringScheduleId, s.SessionDate })
                .ToListAsync();

            var existingSessionsSet = new HashSet<(long RecurringScheduleId, DateOnly SessionDate)>
                (existingSessionsQuery.Select(e => (e.RecurringScheduleId, e.SessionDate)));

            // Dynamic Timezone Handling
            string timezoneId = "Asia/Yangon";
            TimeZoneInfo localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);

            var newSessions = new List<TblSession>();
            int createdCount = 0;

            foreach (var schedule in schedules)
            {
                // Find the first date matching the day of the week constraint mathematically
                int daysToAdd = ((int)schedule.DayOfWeek - (int)semester.StartDate.DayOfWeek + 7) % 7;
                var firstDate = semester.StartDate.AddDays(daysToAdd);

                // Iterate only over target weekdays
                for (var date = firstDate; date <= semester.EndDate; date = date.AddDays(7))
                {
                    if (!existingSessionsSet.Contains((schedule.Id, date)))
                    {
                        var status = holidaySet.Contains(date) ? "Holiday" : "Not Marked";

                        // Convert local time to UTC dynamically based on TimeZoneInfo rules
                        var startDateTimeLocal = new DateTime(date.Year, date.Month, date.Day, schedule.StartTime.Hour, schedule.StartTime.Minute, schedule.StartTime.Second);
                        var endDateTimeLocal = new DateTime(date.Year, date.Month, date.Day, schedule.EndTime.Hour, schedule.EndTime.Minute, schedule.EndTime.Second);

                        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startDateTimeLocal, localTimeZone);
                        var endUtc = TimeZoneInfo.ConvertTimeToUtc(endDateTimeLocal, localTimeZone);

                        var sessionToken = Guid.NewGuid();

                        var session = new TblSession
                        {
                            RecurringScheduleId = schedule.Id,
                            ModuleId = schedule.ModuleId,
                            SemesterId = semester.Id,
                            SessionDate = date,
                            StartDatetime = startUtc,
                            EndDatetime = endUtc,
                            Status = status,
                            MagicLinkToken = sessionToken,
                            GoogleEventId = null,
                            IsDeleted = false
                        };
                        newSessions.Add(session);
                        createdCount++;
                    }
                }
            }

            // Bulk save generated sessions to database
            if (newSessions.Any())
            {
                _context.TblSessions.AddRange(newSessions);
                await _context.SaveChangesAsync();

                await TriggerDashboardSummaryUpdateAsync(request.SemesterId);

                if (request.SyncWithGoogleCalendar)
                {
                    foreach (var session in newSessions)
                    {
                        if (_backgroundJobClient is not null)
                        {
                            _backgroundJobClient.Enqueue<IClassSessionService>(service => 
                                service.SyncGoogleCalendarEventAsync(session.Id));
                        }
                        else
                        {
                            await SyncGoogleCalendarEventAsync(session.Id);
                        }
                    }
                }
            }

            return Result.Success($"Successfully generated {createdCount} new class sessions.");
        }
        catch (Exception)
        {
            return Result.Failure("Failed to generate sessions due to an unexpected error.");
        }
    }
    #endregion

    #region Sync Google Calendar Event
    public async Task SyncGoogleCalendarEventAsync(long sessionId)
    {
        var sessionData = await _context.TblSessions
            .AsNoTracking()
            .Where(s => s.Id == sessionId && !s.IsDeleted)
            .Select(s => new
            {
                s.StartDatetime,
                s.EndDatetime,
                s.MagicLinkToken,
                ModuleName = s.Module.Name
            })
            .FirstOrDefaultAsync();

        if (sessionData == null) return;

        var baseUrl = _configuration?["BaseUrl"] ?? "https://localhost:7119";
        var description = $"Module: {sessionData.ModuleName}\n\nMark Attendance: {baseUrl.TrimEnd('/')}/api/attendance/magic-link/{sessionData.MagicLinkToken}";

        var googleResult = await _googleCalendarService.CreateEventAsync(
            sessionData.ModuleName, 
            sessionData.StartDatetime, 
            sessionData.EndDatetime, 
            description, 
            sessionData.MagicLinkToken);

        if (googleResult.IsSuccess)
        {
            await _context.TblSessions
                .Where(s => s.Id == sessionId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.GoogleEventId, googleResult.Data));
        }
    }
    #endregion

    #region Update Session Status
    public async Task<Result> UpdateSessionStatusAsync(long id, UpdateSessionStatusRequest request)
    {
        try
        {
            var session = await _context.TblSessions.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
            if (session == null) return Result.Failure("Session not found.");

            if (DateTime.UtcNow > session.StartDatetime.AddHours(24))
            {
                return Result.Failure("Attendance status cannot be changed after 24 hours.");
            }

            session.Status = request.Status;
            await _context.SaveChangesAsync();

            await TriggerDashboardSummaryUpdateAsync(session.SemesterId);

            if (request.Status == "Cancelled" && !string.IsNullOrEmpty(session.GoogleEventId))
            {
                await _googleCalendarService.UpdateEventStatusAsync(session.GoogleEventId, "Cancelled");
            }

            return Result.Success("Session status updated successfully.");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to update session status: {ex.Message}");
        }
    }
    #endregion

    #region Mark Attendance with Magic Link
    public async Task<Result<string>> MarkAttendanceWithMagicLinkAsync(Guid token)
    {
        try
        {
            var session = await _context.TblSessions.FirstOrDefaultAsync(s => s.MagicLinkToken == token && !s.IsDeleted);
            if (session == null) return Result<string>.Failure("Invalid attendance link.");

            var nowUtc = DateTime.UtcNow;

            // Allow 15 mins before to 1 hour after
            var validFrom = session.StartDatetime.AddMinutes(-15);
            var validTo = session.EndDatetime.AddHours(1);

            if (nowUtc < validFrom || nowUtc > validTo)
            {
                return Result<string>.Failure("Outside of attendance window.");
            }

            if (DateTime.UtcNow > session.StartDatetime.AddHours(24))
            {
                return Result<string>.Failure("Attendance status cannot be changed after 24 hours.");
            }

            if (session.Status == "Present")
            {
                return Result<string>.Success("Already marked as present.");
            }

            session.Status = "Present";
            await _context.SaveChangesAsync();

            await TriggerDashboardSummaryUpdateAsync(session.SemesterId);

            return Result<string>.Success("Attendance successfully marked.");
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Error processing attendance link: {ex.Message}");
        }
    }
    #endregion

    #region Update Session
    public async Task<Result> UpdateSessionAsync(long id, UpdateClassSessionRequest request)
    {
        try
        {
            var session = await _context.TblSessions.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
            if (session == null) return Result.Failure("Session not found.");

            if (DateTime.UtcNow > session.StartDatetime.AddHours(24))
            {
                return Result.Failure("Attendance status cannot be changed after 24 hours.");
            }

            var moduleExists = await _context.TblModules.AnyAsync(m => m.Id == request.ModuleId && !m.IsDeleted);
            if (!moduleExists) return Result.Failure("Module not found.");

            session.ModuleId = request.ModuleId;
            session.SessionDate = request.SessionDate;
            session.StartDatetime = DateTime.SpecifyKind(request.StartDatetime, DateTimeKind.Utc);
            session.EndDatetime = DateTime.SpecifyKind(request.EndDatetime, DateTimeKind.Utc);

            var oldStatus = session.Status;
            session.Status = request.Status;

            await _context.SaveChangesAsync();

            await TriggerDashboardSummaryUpdateAsync(session.SemesterId);

            if (request.Status == "Cancelled" && !string.IsNullOrEmpty(session.GoogleEventId))
            {
                await _googleCalendarService.UpdateEventStatusAsync(session.GoogleEventId, "Cancelled");
            }
            else if (oldStatus == "Cancelled" && request.Status != "Cancelled" && !string.IsNullOrEmpty(session.GoogleEventId))
            {
                await _googleCalendarService.UpdateEventStatusAsync(session.GoogleEventId, "Confirmed");
            }

            return Result.Success("Session updated successfully.");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to update session: {ex.Message}");
        }
    }
    #endregion

    #region Delete Session
    public async Task<Result> DeleteSessionAsync(long id)
    {
        try
        {
            var session = await _context.TblSessions.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
            if (session == null) return Result.Failure("Session not found.");

            if (DateTime.UtcNow > session.StartDatetime.AddHours(24))
            {
                return Result.Failure("Attendance status cannot be changed after 24 hours.");
            }

            session.IsDeleted = true;
            await _context.SaveChangesAsync();

            await TriggerDashboardSummaryUpdateAsync(session.SemesterId);

            if (!string.IsNullOrEmpty(session.GoogleEventId))
            {
                await _googleCalendarService.DeleteEventAsync(session.GoogleEventId);
            }

            return Result.Success("Session deleted successfully.");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete session: {ex.Message}");
        }
    }
    #endregion

    private async Task TriggerDashboardSummaryUpdateAsync(long semesterId)
    {
        if (_backgroundJobClient is not null)
        {
            _backgroundJobClient.Enqueue<IAnalyticsService>(service => service.UpdateSemesterDashboardSummaryAsync(semesterId));
        }
        else if (_serviceProvider is not null)
        {
            using var scope = _serviceProvider.CreateScope();
            var analyticsService = scope.ServiceProvider.GetService<IAnalyticsService>();
            if (analyticsService is not null)
            {
                await analyticsService.UpdateSemesterDashboardSummaryAsync(semesterId);
            }
        }
    }
}
