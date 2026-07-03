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

namespace ACST.Domain.Features.RecurringSchedules;

public class RecurringScheduleService : IRecurringScheduleService
{
    private readonly AppDbContext _context;
    private readonly IClassSessionService _classSessionService;
    private readonly IGoogleCalendarService _googleCalendarService;
    private readonly IBackgroundJobClient? _backgroundJobClient;

    public RecurringScheduleService(
        AppDbContext context, 
        IClassSessionService classSessionService, 
        IGoogleCalendarService googleCalendarService,
        IBackgroundJobClient? backgroundJobClient = null)
    {
        _context = context;
        _classSessionService = classSessionService;
        _googleCalendarService = googleCalendarService;
        _backgroundJobClient = backgroundJobClient;
    }

    #region Get schedules by module
    public async Task<Result<IEnumerable<RecurringScheduleDto>>> GetSchedulesByModuleAsync(long moduleId)
    {
        try
        {
            var schedules = await _context.TblRecurringSchedules
                .Include(r => r.Module)
                .Include(r => r.Semester)
                .AsNoTracking()
                .Where(r => r.ModuleId == moduleId && !r.IsDeleted)
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

            var semesterExists = await _context.TblSemesters.AnyAsync(s => s.Id == semesterId && !s.IsDeleted);
            if (!semesterExists)
            {
                return Result<RecurringScheduleDto>.Failure("Semester not found.");
            }

            if (request.StartTime >= request.EndTime)
            {
                return Result<RecurringScheduleDto>.Failure("Start time must be before end time.");
            }

            var schedule = new TblRecurringSchedule
            {
                ModuleId = moduleId,
                SemesterId = semesterId,
                DayOfWeek = request.DayOfWeek,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                IsActive = true,
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

    #region Delete Schedule
    public async Task<Result> DeleteScheduleAsync(long id)
    {
        try
        {
            var schedule = await _context.TblRecurringSchedules.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
            if (schedule == null) return Result.Failure("Recurring schedule not found.");

            schedule.IsDeleted = true;

            // Cascade soft-delete future class sessions for this deleted schedule
            var associatedSessions = await _context.TblSessions
                .Where(s => s.RecurringScheduleId == id && !s.IsDeleted && s.StartDatetime >= DateTime.UtcNow)
                .ToListAsync();
            foreach (var s in associatedSessions)
            {
                s.IsDeleted = true;
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
