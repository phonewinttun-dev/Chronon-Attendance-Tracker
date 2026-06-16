using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.ClassSession;
using ACST.Domain.Features.GoogleCalendar;
using ACST.Shared;
using Microsoft.EntityFrameworkCore;

namespace ACST.Domain.Features.ClassSessions;

public class ClassSessionService : IClassSessionService
{
    private readonly AppDbContext _context;
    private readonly IGoogleCalendarService _googleCalendarService;
    private static readonly TimeSpan MyanmarOffset = TimeSpan.FromHours(6.5);

    public ClassSessionService(AppDbContext context, IGoogleCalendarService googleCalendarService)
    {
        _context = context;
        _googleCalendarService = googleCalendarService;
    }

    public async Task<PagedResult<ClassSessionDto>> GetSessionsAsync(long? semesterId, long? moduleId, DateOnly? startDate, DateOnly? endDate, string? status, int? dayOfWeek = null, int? pageNumber = null, int? pageSize = null)
    {
        try
        {
            var query = _context.TblSessions
                .Include(s => s.Module)
                .Include(s => s.Semester)
                .AsNoTracking()
                .Where(s => !s.IsDeleted);

            if (semesterId.HasValue) query = query.Where(s => s.SemesterId == semesterId.Value);
            if (moduleId.HasValue) query = query.Where(s => s.ModuleId == moduleId.Value);
            if (startDate.HasValue) query = query.Where(s => s.SessionDate >= startDate.Value);
            if (endDate.HasValue) query = query.Where(s => s.SessionDate <= endDate.Value);
            if (!string.IsNullOrEmpty(status)) query = query.Where(s => s.Status == status);
            if (dayOfWeek.HasValue) query = query.Where(s => (int)s.SessionDate.DayOfWeek == dayOfWeek.Value);

            int totalCount = await query.CountAsync();
            List<ClassSessionDto> items;
            Pagination pagination;

            var orderedQuery = query.OrderBy(s => s.StartDatetime);

            if (pageNumber.HasValue && pageSize.HasValue)
            {
                items = await orderedQuery
                    .Skip((pageNumber.Value - 1) * pageSize.Value)
                    .Take(pageSize.Value)
                    .Select(s => new ClassSessionDto
                    {
                        Id = s.Id,
                        RecurringScheduleId = s.RecurringScheduleId,
                        ModuleId = s.ModuleId,
                        ModuleName = s.Module.Name,
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

                pagination = new Pagination(pageNumber.Value, pageSize.Value, totalCount);
            }
            else
            {
                items = await orderedQuery
                    .Select(s => new ClassSessionDto
                    {
                        Id = s.Id,
                        RecurringScheduleId = s.RecurringScheduleId,
                        ModuleId = s.ModuleId,
                        ModuleName = s.Module.Name,
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

                pagination = new Pagination(1, totalCount > 0 ? totalCount : 1, totalCount);
            }

            return PagedResult<ClassSessionDto>.Success(items, pagination);
        }
        catch (Exception ex)
        {
            return PagedResult<ClassSessionDto>.Failure($"Failed to retrieve sessions: {ex.Message}");
        }
    }

    public async Task<Result<ClassSessionDto>> GetSessionByIdAsync(long id)
    {
        try
        {
            var s = await _context.TblSessions
                .Include(s => s.Module)
                .Include(s => s.Semester)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (s == null) return Result<ClassSessionDto>.Failure("Session not found.");

            return Result<ClassSessionDto>.Success(new ClassSessionDto
            {
                Id = s.Id,
                RecurringScheduleId = s.RecurringScheduleId,
                ModuleId = s.ModuleId,
                ModuleName = s.Module.Name,
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

    public async Task<Result> GenerateSessionsAsync(GenerateSessionsRequest request)
    {
        try
        {
            var semester = await _context.TblSemesters.FirstOrDefaultAsync(s => s.Id == request.SemesterId && !s.IsDeleted);
            if (semester == null) return Result.Failure("Semester not found.");

            var schedulesQuery = _context.TblRecurringSchedules
                .Include(r => r.Module)
                .Where(r => r.SemesterId == request.SemesterId && r.IsActive && !r.IsDeleted);

            if (request.ModuleId.HasValue)
            {
                schedulesQuery = schedulesQuery.Where(r => r.ModuleId == request.ModuleId.Value);
            }

            var schedules = await schedulesQuery.ToListAsync();
            if (!schedules.Any()) return Result.Failure("No active recurring schedules found for this semester.");

            var holidays = await _context.TblHolidays
                .Where(h => !h.IsDeleted && h.HolidayDate >= semester.StartDate && h.HolidayDate <= semester.EndDate)
                .Select(h => h.HolidayDate)
                .ToListAsync();

            var existingSessions = await _context.TblSessions
                .Where(s => s.SemesterId == semester.Id && !s.IsDeleted)
                .Select(s => new { s.RecurringScheduleId, s.SessionDate })
                .ToListAsync();

            var newSessions = new List<TblSession>();
            int createdCount = 0;

            foreach (var schedule in schedules)
            {
                for (var date = semester.StartDate; date <= semester.EndDate; date = date.AddDays(1))
                {
                    if ((short)date.DayOfWeek == schedule.DayOfWeek)
                    {
                        bool exists = existingSessions.Any(e => e.RecurringScheduleId == schedule.Id && e.SessionDate == date);
                        if (!exists)
                        {
                            var status = holidays.Contains(date) ? "Holiday" : "Not Marked";

                            // Convert local time in Myanmar to UTC
                            var startDateTimeLocal = new DateTime(date.Year, date.Month, date.Day, schedule.StartTime.Hour, schedule.StartTime.Minute, schedule.StartTime.Second);
                            var endDateTimeLocal = new DateTime(date.Year, date.Month, date.Day, schedule.EndTime.Hour, schedule.EndTime.Minute, schedule.EndTime.Second);
                            
                            var startUtc = startDateTimeLocal - MyanmarOffset;
                            var endUtc = endDateTimeLocal - MyanmarOffset;

                            var sessionToken = Guid.NewGuid();

                            // Mock google event creation
                            var description = $"Module: {schedule.Module.Name}\n\nMark Attendance: https://localhost:7119/api/attendance/magic-link/{sessionToken}";
                            var googleResult = await _googleCalendarService.CreateEventAsync(schedule.Module.Name, startUtc, endUtc, description, sessionToken);

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
                                GoogleEventId = googleResult.IsSuccess ? googleResult.Data : null,
                                IsDeleted = false
                            };
                            newSessions.Add(session);
                            createdCount++;
                        }
                    }
                }
            }

            if (newSessions.Any())
            {
                _context.TblSessions.AddRange(newSessions);
                await _context.SaveChangesAsync();
            }

            return Result.Success($"Successfully generated {createdCount} new class sessions.");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to generate sessions: {ex.Message}");
        }
    }

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
            _context.TblSessions.Update(session);
            await _context.SaveChangesAsync();

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
            _context.TblSessions.Update(session);
            await _context.SaveChangesAsync();

            return Result<string>.Success("Attendance successfully marked.");
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Error processing attendance link: {ex.Message}");
        }
    }

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
            session.StartDatetime = request.StartDatetime;
            session.EndDatetime = request.EndDatetime;
            
            var oldStatus = session.Status;
            session.Status = request.Status;

            _context.TblSessions.Update(session);
            await _context.SaveChangesAsync();

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
            _context.TblSessions.Update(session);
            await _context.SaveChangesAsync();

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
}
