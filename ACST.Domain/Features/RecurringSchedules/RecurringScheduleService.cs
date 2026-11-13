using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.RecurringSchedule;
using ACST.Shared;
using Microsoft.EntityFrameworkCore;
using Hangfire;

using ACST.Domain.Features.ClassSessions;
using ACST.Domain.Features.GoogleCalendar;
using ACST.Domain.DTOs.ClassSession;

using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ACST.Domain.Features.RecurringSchedules;

public class RecurringScheduleService : IRecurringScheduleService
{
    private readonly AppDbContext _context;
    private readonly IClassSessionService _classSessionService;
    private readonly IGoogleCalendarService _googleCalendarService;
    private readonly IBackgroundJobClient? _backgroundJobClient;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public RecurringScheduleService(
        AppDbContext context, 
        IClassSessionService classSessionService, 
        IGoogleCalendarService googleCalendarService,
        IBackgroundJobClient? backgroundJobClient = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _context = context;
        _classSessionService = classSessionService;
        _googleCalendarService = googleCalendarService;
        _backgroundJobClient = backgroundJobClient;
        _httpContextAccessor = httpContextAccessor;
    }

    private int? CurrentUserId
    {
        get
        {
            var claim = _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var userId) ? userId : null;
        }
    }

    private bool IsAdmin => _httpContextAccessor?.HttpContext?.User?.IsInRole("Admin") ?? false;

    private bool CanAccess(TblRecurringSchedule schedule)
    {
        if (_httpContextAccessor?.HttpContext == null || IsAdmin) return true;
        return CurrentUserId.HasValue && schedule.UserId == CurrentUserId.Value;
    }

    #region Get schedules by module
    public async Task<Result<IEnumerable<RecurringScheduleDto>>> GetSchedulesByModuleAsync(long moduleId)
    {
        try
        {
            var query = _context.TblRecurringSchedules
                .Include(r => r.Module)
                .Include(r => r.Semester)
                .AsNoTracking()
                .Where(r => r.ModuleId == moduleId && !r.IsDeleted);

            if (_httpContextAccessor?.HttpContext != null && !IsAdmin)
            {
                if (CurrentUserId.HasValue)
                {
                    query = query.Where(r => r.UserId == CurrentUserId.Value);
                }
                else
                {
                    return Result<IEnumerable<RecurringScheduleDto>>.Failure("Unauthorized access.");
                }
            }

            var schedules = await query
                .Select(r => new RecurringScheduleDto
                {
                    Id = r.Id,
                    ModuleId = r.ModuleId,
                    ModuleName = r.Module.Name,
                    SemesterId = r.SemesterId,
                    SemesterName = r.Semester.Name,
                    DayOfWeek = r.DayOfWeek,
                    StartTime = r.StartTime,
                    EndTime = r.EndTime,
                    EffectiveStartDate = r.EffectiveStartDate,
                    EffectiveEndDate = r.EffectiveEndDate,
                    IsActive = r.IsActive,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .ToListAsync();

            return Result<IEnumerable<RecurringScheduleDto>>.Success(schedules);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<RecurringScheduleDto>>.Failure($"Failed to retrieve recurring schedules: {ex.Message}");
        }
    }
    #endregion

    #region Create schedule
    public async Task<Result<RecurringScheduleDto>> CreateScheduleAsync(long moduleId, long semesterId, CreateRecurringScheduleRequest request)
    {
        try
        {
            var moduleExists = await _context.TblModules.AnyAsync(m => m.Id == moduleId && !m.IsDeleted);
            if (!moduleExists)
            {
                return Result<RecurringScheduleDto>.Failure("Module not found.");
            }

            var semester = await _context.TblSemesters.FirstOrDefaultAsync(s => s.Id == semesterId && !s.IsDeleted);
            if (semester == null)
            {
                return Result<RecurringScheduleDto>.Failure("Semester not found.");
            }

            if (request.StartTime >= request.EndTime)
            {
                return Result<RecurringScheduleDto>.Failure("Start time must be before end time.");
            }

            if (request.EffectiveStartDate.HasValue && request.EffectiveEndDate.HasValue && request.EffectiveStartDate.Value > request.EffectiveEndDate.Value)
            {
                return Result<RecurringScheduleDto>.Failure("Effective start date must be on or before effective end date.");
            }

            if (request.EffectiveStartDate.HasValue && request.EffectiveStartDate.Value < semester.StartDate)
            {
                return Result<RecurringScheduleDto>.Failure("Effective start date cannot be before semester start date.");
            }

            if (request.EffectiveEndDate.HasValue && request.EffectiveEndDate.Value > semester.EndDate)
            {
                return Result<RecurringScheduleDto>.Failure("Effective end date cannot be after semester end date.");
            }

            var schedule = new TblRecurringSchedule
            {
                ModuleId = moduleId,
                SemesterId = semesterId,
                DayOfWeek = request.DayOfWeek,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                EffectiveStartDate = request.EffectiveStartDate,
                EffectiveEndDate = request.EffectiveEndDate,
                IsActive = true,
                UserId = CurrentUserId,
                IsDeleted = false
            };

            _context.TblRecurringSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            if (request.GenerateSessions)
            {
                await _classSessionService.GenerateSessionsAsync(new GenerateSessionsRequest
                {
                    SemesterId = semesterId,
                    ModuleId = moduleId,
                    SyncWithGoogleCalendar = request.SyncWithGoogleCalendar
                });
            }

            var created = await _context.TblRecurringSchedules
                .Include(r => r.Module)
                .Include(r => r.Semester)
                .FirstAsync(r => r.Id == schedule.Id);

            return Result<RecurringScheduleDto>.Success(new RecurringScheduleDto
            {
                Id = created.Id,
                ModuleId = created.ModuleId,
                ModuleName = created.Module.Name,
                SemesterId = created.SemesterId,
                SemesterName = created.Semester.Name,
                DayOfWeek = created.DayOfWeek,
                StartTime = created.StartTime,
                EndTime = created.EndTime,
                EffectiveStartDate = created.EffectiveStartDate,
                EffectiveEndDate = created.EffectiveEndDate,
                IsActive = created.IsActive,
                CreatedAt = created.CreatedAt,
                UpdatedAt = created.UpdatedAt
            }, "Recurring schedule created successfully.");
        }
        catch (Exception ex)
        {
            return Result<RecurringScheduleDto>.Failure($"Failed to create recurring schedule: {ex.Message}");
        }
    }
    #endregion

    #region Update schedule
    public async Task<Result<RecurringScheduleDto>> UpdateScheduleAsync(long id, UpdateRecurringScheduleRequest request)
    {
        try
        {
            var schedule = await _context.TblRecurringSchedules
                .Include(r => r.Module)
                .Include(r => r.Semester)
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

            if (schedule == null || !CanAccess(schedule))
            {
                return Result<RecurringScheduleDto>.Failure("Recurring schedule not found.");
            }

            if (request.StartTime >= request.EndTime)
            {
                return Result<RecurringScheduleDto>.Failure("Start time must be before end time.");
            }

            if (request.EffectiveStartDate.HasValue && request.EffectiveEndDate.HasValue && request.EffectiveStartDate.Value > request.EffectiveEndDate.Value)
            {
                return Result<RecurringScheduleDto>.Failure("Effective start date must be on or before effective end date.");
            }

            if (request.EffectiveStartDate.HasValue && request.EffectiveStartDate.Value < schedule.Semester.StartDate)
            {
                return Result<RecurringScheduleDto>.Failure("Effective start date cannot be before semester start date.");
            }

            if (request.EffectiveEndDate.HasValue && request.EffectiveEndDate.Value > schedule.Semester.EndDate)
            {
                return Result<RecurringScheduleDto>.Failure("Effective end date cannot be after semester end date.");
            }

            schedule.DayOfWeek = request.DayOfWeek;
            schedule.StartTime = request.StartTime;
            schedule.EndTime = request.EndTime;
            schedule.EffectiveStartDate = request.EffectiveStartDate;
            schedule.EffectiveEndDate = request.EffectiveEndDate;
            schedule.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Result<RecurringScheduleDto>.Success(new RecurringScheduleDto
            {
                Id = schedule.Id,
                ModuleId = schedule.ModuleId,
                ModuleName = schedule.Module.Name,
                SemesterId = schedule.SemesterId,
                SemesterName = schedule.Semester.Name,
                DayOfWeek = schedule.DayOfWeek,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                EffectiveStartDate = schedule.EffectiveStartDate,
                EffectiveEndDate = schedule.EffectiveEndDate,
                IsActive = schedule.IsActive,
                CreatedAt = schedule.CreatedAt,
                UpdatedAt = schedule.UpdatedAt
            }, "Recurring schedule updated successfully.");
        }
        catch (Exception ex)
        {
            return Result<RecurringScheduleDto>.Failure($"Failed to update recurring schedule: {ex.Message}");
        }
    }
    #endregion

    #region Delete Schedule
    public async Task<Result> DeleteScheduleAsync(long id)
    {
        try
        {
            var schedule = await _context.TblRecurringSchedules.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
            if (schedule == null || !CanAccess(schedule)) return Result.Failure("Recurring schedule not found.");

            schedule.IsDeleted = true;

            // Cascade soft-delete future unmarked class sessions for this deleted schedule
            // Retain any session that has already been marked (Present or Absent) to preserve attendance records
            var associatedSessions = await _context.TblSessions
                .Where(s => s.RecurringScheduleId == id && !s.IsDeleted && s.StartDatetime >= DateTime.UtcNow && s.Status == "Not Marked")
                .ToListAsync();
            foreach (var s in associatedSessions)
            {
                s.IsDeleted = true;
                /*
                if (!string.IsNullOrEmpty(s.GoogleEventId))
                {
                    if (_backgroundJobClient is not null)
                    {
                        _backgroundJobClient.Enqueue<IGoogleCalendarService>(service => 
                            service.DeleteEventAsync(s.GoogleEventId));
                    }
                    else
                    {
                        await _googleCalendarService.DeleteEventAsync(s.GoogleEventId);
                    }
                }
                */
            }

            await _context.SaveChangesAsync();

            return Result.Success("Recurring schedule deleted successfully.");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete recurring schedule: {ex.Message}");
        }
    }
    #endregion
}
