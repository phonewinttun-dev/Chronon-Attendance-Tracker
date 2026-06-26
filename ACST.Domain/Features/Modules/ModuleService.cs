using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.Module;
using ACST.Domain.DTOs.RecurringSchedule;
using ACST.Domain.Features.GoogleCalendar;
using ACST.Shared;
using Microsoft.EntityFrameworkCore;

using ACST.Domain.Features.ClassSessions;
using ACST.Domain.DTOs.ClassSession;

namespace ACST.Domain.Features.Modules;

public class ModuleService : IModuleService
{
    private readonly AppDbContext _context;
    private readonly IGoogleCalendarService _googleCalendarService;
    private readonly IClassSessionService _classSessionService;

    public ModuleService(AppDbContext context, IGoogleCalendarService googleCalendarService, IClassSessionService classSessionService)
    {
        _context = context;
        _googleCalendarService = googleCalendarService;
        _classSessionService = classSessionService;
    }

    public async Task<PagedResult<ModuleDto>> GetAllModulesAsync(int? pageNumber = null, int? pageSize = null, long? semesterId = null)
    {
        try
        {
            var query = _context.TblModules
                .AsNoTracking()
                .Include(m => m.Semester)
                .Where(m => !m.IsDeleted);

            if (semesterId.HasValue)
            {
                query = query.Where(m => m.SemesterId == semesterId.Value);
            }

            query = query.OrderBy(m => m.Name);

            int totalCount = await query.CountAsync();
            List<ModuleDto> items;
            Pagination pagination;

            if (pageNumber.HasValue && pageSize.HasValue)
            {
                var rawItems = await query
                    .Select(m => new
                    {
                        m.Id,
                        m.Name,
                        m.ModuleCode,
                        m.TeacherName,
                        m.SemesterId,
                        SemesterName = m.Semester != null ? m.Semester.Name : null,
                        TotalValidSessions = m.TblSessions.Count(s => !s.IsDeleted && s.Status != "Holiday" && s.Status != "Cancelled"),
                        PresentSessions = m.TblSessions.Count(s => !s.IsDeleted && s.Status == "Present"),
                        Schedules = m.TblRecurringSchedules
                            .Where(s => !s.IsDeleted)
                            .Select(s => new RecurringScheduleDto
                            {
                                Id = s.Id,
                                ModuleId = s.ModuleId,
                                ModuleName = m.Name,
                                SemesterId = s.SemesterId,
                                SemesterName = s.Semester.Name,
                                DayOfWeek = s.DayOfWeek,
                                StartTime = s.StartTime,
                                EndTime = s.EndTime
                            }).ToList(),
                        m.CreatedAt,
                        m.UpdatedAt
                    })
                    .Skip((pageNumber.Value - 1) * pageSize.Value)
                    .Take(pageSize.Value)
                    .ToListAsync();

                items = rawItems.Select(m => new ModuleDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    ModuleCode = m.ModuleCode,
                    TeacherName = m.TeacherName,
                    SemesterId = m.SemesterId,
                    SemesterName = m.SemesterName,
                    TotalValidSessions = m.TotalValidSessions,
                    PresentSessions = m.PresentSessions,
                    AttendanceRate = m.TotalValidSessions > 0 ? Math.Round((double)m.PresentSessions / m.TotalValidSessions * 100, 2) : 0,
                    Schedules = m.Schedules,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                }).ToList();

                pagination = new Pagination(pageNumber.Value, pageSize.Value, totalCount);
            }
            else
            {
                var rawItems = await query
                    .Select(m => new
                    {
                        m.Id,
                        m.Name,
                        m.ModuleCode,
                        m.TeacherName,
                        m.SemesterId,
                        SemesterName = m.Semester != null ? m.Semester.Name : null,
                        TotalValidSessions = m.TblSessions.Count(s => !s.IsDeleted && s.Status != "Holiday" && s.Status != "Cancelled"),
                        PresentSessions = m.TblSessions.Count(s => !s.IsDeleted && s.Status == "Present"),
                        Schedules = m.TblRecurringSchedules
                            .Where(s => !s.IsDeleted)
                            .Select(s => new RecurringScheduleDto
                            {
                                Id = s.Id,
                                ModuleId = s.ModuleId,
                                ModuleName = m.Name,
                                SemesterId = s.SemesterId,
                                SemesterName = s.Semester.Name,
                                DayOfWeek = s.DayOfWeek,
                                StartTime = s.StartTime,
                                EndTime = s.EndTime
                            }).ToList(),
                        CreatedAt = m.CreatedAt,
                        UpdatedAt = m.UpdatedAt
                    })
                    .ToListAsync();

                items = rawItems.Select(m => new ModuleDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    ModuleCode = m.ModuleCode,
                    TeacherName = m.TeacherName,
                    SemesterId = m.SemesterId,
                    SemesterName = m.SemesterName,
                    TotalValidSessions = m.TotalValidSessions,
                    PresentSessions = m.PresentSessions,
                    AttendanceRate = m.TotalValidSessions > 0 ? Math.Round((double)m.PresentSessions / m.TotalValidSessions * 100, 2) : 0,
                    Schedules = m.Schedules,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                }).ToList();

                pagination = new Pagination(1, totalCount > 0 ? totalCount : 1, totalCount);
            }

            return PagedResult<ModuleDto>.Success(items, pagination);
        }
        catch (Exception ex)
        {
            return PagedResult<ModuleDto>.Failure($"Failed to retrieve modules: {ex.Message}");
        }
    }

    public async Task<Result<ModuleDto>> GetModuleByIdAsync(long id)
    {
        try
        {
            var moduleData = await _context.TblModules
                .AsNoTracking()
                .Where(m => m.Id == id && !m.IsDeleted)
                .Select(m => new
                {
                    m.Id,
                    m.Name,
                    m.ModuleCode,
                    m.TeacherName,
                    m.SemesterId,
                    SemesterName = m.Semester != null ? m.Semester.Name : null,
                    TotalValidSessions = m.TblSessions.Count(s => !s.IsDeleted && s.Status != "Holiday" && s.Status != "Cancelled"),
                    PresentSessions = m.TblSessions.Count(s => !s.IsDeleted && s.Status == "Present"),
                    Schedules = m.TblRecurringSchedules
                        .Where(s => !s.IsDeleted)
                        .Select(s => new RecurringScheduleDto
                        {
                            Id = s.Id,
                            ModuleId = s.ModuleId,
                            ModuleName = m.Name,
                            SemesterId = s.SemesterId,
                            SemesterName = s.Semester.Name,
                            DayOfWeek = s.DayOfWeek,
                            StartTime = s.StartTime,
                            EndTime = s.EndTime
                        }).ToList(),
                    m.CreatedAt,
                    m.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (moduleData == null)
            {
                return Result<ModuleDto>.Failure("Module not found.");
            }

            return Result<ModuleDto>.Success(new ModuleDto
            {
                Id = moduleData.Id,
                Name = moduleData.Name,
                ModuleCode = moduleData.ModuleCode,
                TeacherName = moduleData.TeacherName,
                SemesterId = moduleData.SemesterId,
                SemesterName = moduleData.SemesterName,
                TotalValidSessions = moduleData.TotalValidSessions,
                PresentSessions = moduleData.PresentSessions,
                AttendanceRate = moduleData.TotalValidSessions > 0 ? Math.Round((double)moduleData.PresentSessions / moduleData.TotalValidSessions * 100, 2) : 0,
                Schedules = moduleData.Schedules,
                CreatedAt = moduleData.CreatedAt,
                UpdatedAt = moduleData.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            return Result<ModuleDto>.Failure($"Error retrieving module: {ex.Message}");
        }
    }

    public async Task<Result<ModuleDto>> CreateModuleAsync(CreateModuleRequest request)
    {
        try
        {
            string? semesterName = null;
            if (request.SemesterId.HasValue)
            {
                var semester = await _context.TblSemesters.FirstOrDefaultAsync(s => s.Id == request.SemesterId.Value && !s.IsDeleted);
                if (semester == null)
                {
                    return Result<ModuleDto>.Failure("Selected semester does not exist or is deleted.");
                }
                semesterName = semester.Name;
            }

            var module = new TblModule
            {
                Name = request.Name,
                ModuleCode = request.ModuleCode,
                TeacherName = request.TeacherName,
                SemesterId = request.SemesterId,
                IsDeleted = false
            };

            _context.TblModules.Add(module);
            await _context.SaveChangesAsync();

            // Save schedules if any
            if (request.Schedules != null && request.Schedules.Any())
            {
                if (!request.SemesterId.HasValue)
                {
                    return Result<ModuleDto>.Failure("Semester is required when adding schedules.");
                }

                foreach (var schRequest in request.Schedules)
                {
                    if (schRequest.StartTime >= schRequest.EndTime)
                    {
                        return Result<ModuleDto>.Failure("Start time must be before end time for all schedules.");
                    }

                    var schedule = new TblRecurringSchedule
                    {
                        ModuleId = module.Id,
                        SemesterId = request.SemesterId.Value,
                        DayOfWeek = schRequest.DayOfWeek,
                        StartTime = schRequest.StartTime,
                        EndTime = schRequest.EndTime,
                        IsActive = true,
                        IsDeleted = false
                    };
                    _context.TblRecurringSchedules.Add(schedule);
                }
                await _context.SaveChangesAsync();

                if (request.GenerateSessions)
                {
                    await _classSessionService.GenerateSessionsAsync(new GenerateSessionsRequest
                    {
                        SemesterId = request.SemesterId.Value,
                        ModuleId = module.Id,
                        SyncWithGoogleCalendar = request.SyncWithGoogleCalendar
                    });
                }
            }

            var schedulesList = new List<RecurringScheduleDto>();
            if (request.Schedules != null && request.Schedules.Any() && request.SemesterId.HasValue)
            {
                schedulesList = await _context.TblRecurringSchedules
                    .Include(s => s.Semester)
                    .Where(s => s.ModuleId == module.Id && !s.IsDeleted)
                    .Select(s => new RecurringScheduleDto
                    {
                        Id = s.Id,
                        ModuleId = s.ModuleId,
                        ModuleName = module.Name,
                        SemesterId = s.SemesterId,
                        SemesterName = s.Semester.Name,
                        DayOfWeek = s.DayOfWeek,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        IsActive = s.IsActive,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt
                    })
                    .ToListAsync();
            }

            return Result<ModuleDto>.Success(new ModuleDto
            {
                Id = module.Id,
                Name = module.Name,
                ModuleCode = module.ModuleCode,
                TeacherName = module.TeacherName,
                SemesterId = module.SemesterId,
                SemesterName = semesterName,
                AttendanceRate = 0,
                TotalValidSessions = 0,
                PresentSessions = 0,
                Schedules = schedulesList,
                CreatedAt = module.CreatedAt,
                UpdatedAt = module.UpdatedAt
            }, "Module created successfully.");
        }
        catch (Exception ex)
        {
            return Result<ModuleDto>.Failure($"Failed to create module: {ex.Message}");
        }
    }

    public async Task<Result<ModuleDto>> UpdateModuleAsync(long id, UpdateModuleRequest request)
    {
        try
        {
            var module = await _context.TblModules.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (module == null)
            {
                return Result<ModuleDto>.Failure("Module not found.");
            }

            string? semesterName = null;
            if (request.SemesterId.HasValue)
            {
                var semester = await _context.TblSemesters.FirstOrDefaultAsync(s => s.Id == request.SemesterId.Value && !s.IsDeleted);
                if (semester == null)
                {
                    return Result<ModuleDto>.Failure("Selected semester does not exist or is deleted.");
                }
                semesterName = semester.Name;
            }

            module.Name = request.Name;
            module.ModuleCode = request.ModuleCode;
            module.TeacherName = request.TeacherName;
            module.SemesterId = request.SemesterId;

            await _context.SaveChangesAsync();

            // Handle schedules update
            if (request.Schedules != null)
            {
                if (request.Schedules.Any() && !request.SemesterId.HasValue)
                {
                    return Result<ModuleDto>.Failure("Semester is required when adding schedules.");
                }

                var existingSchedules = await _context.TblRecurringSchedules
                    .Where(s => s.ModuleId == id && !s.IsDeleted)
                    .ToListAsync();

                // Determine deleted schedules
                var requestIds = request.Schedules.Where(s => s.Id.HasValue).Select(s => s.Id!.Value).ToList();
                var toDelete = existingSchedules.Where(s => !requestIds.Contains(s.Id)).ToList();
                foreach (var sch in toDelete)
                {
                    sch.IsDeleted = true;

                    // Cascade soft-delete future class sessions for this deleted schedule
                    var associatedSessions = await _context.TblSessions
                        .Where(s => s.RecurringScheduleId == sch.Id && !s.IsDeleted && s.StartDatetime >= DateTime.UtcNow)
                        .ToListAsync();
                    foreach (var s in associatedSessions)
                    {
                        s.IsDeleted = true;
                        if (!string.IsNullOrEmpty(s.GoogleEventId))
                        {
                            await _googleCalendarService.DeleteEventAsync(s.GoogleEventId);
                        }
                    }
                }

                // Add or update schedules
                foreach (var schReq in request.Schedules)
                {
                    if (schReq.StartTime >= schReq.EndTime)
                    {
                        return Result<ModuleDto>.Failure("Start time must be before end time for all schedules.");
                    }

                    if (schReq.Id.HasValue)
                    {
                        var existing = existingSchedules.FirstOrDefault(s => s.Id == schReq.Id.Value);
                        if (existing != null)
                        {
                            existing.DayOfWeek = schReq.DayOfWeek;
                            existing.StartTime = schReq.StartTime;
                            existing.EndTime = schReq.EndTime;
                            existing.SemesterId = request.SemesterId!.Value;
                        }
                    }
                    else
                    {
                        var newSch = new TblRecurringSchedule
                        {
                            ModuleId = module.Id,
                            SemesterId = request.SemesterId!.Value,
                            DayOfWeek = schReq.DayOfWeek,
                            StartTime = schReq.StartTime,
                            EndTime = schReq.EndTime,
                            IsActive = true,
                            IsDeleted = false
                        };
                        _context.TblRecurringSchedules.Add(newSch);
                    }
                }
                await _context.SaveChangesAsync();

                if (request.SemesterId.HasValue)
                {
                    await _classSessionService.GenerateSessionsAsync(new GenerateSessionsRequest
                    {
                        SemesterId = request.SemesterId.Value,
                        ModuleId = id
                    });
                }
            }

            var totalValid = await _context.TblSessions.CountAsync(s => s.ModuleId == id && !s.IsDeleted && s.Status != "Holiday" && s.Status != "Cancelled");
            var present = await _context.TblSessions.CountAsync(s => s.ModuleId == id && !s.IsDeleted && s.Status == "Present");
            var rate = totalValid > 0 ? Math.Round((double)present / totalValid * 100, 2) : 0;

            // Fetch current list of schedules to return in DTO
            var schedulesList = await _context.TblRecurringSchedules
                .Include(s => s.Semester)
                .Where(s => s.ModuleId == id && !s.IsDeleted)
                .Select(s => new RecurringScheduleDto
                {
                    Id = s.Id,
                    ModuleId = s.ModuleId,
                    ModuleName = module.Name,
                    SemesterId = s.SemesterId,
                    SemesterName = s.Semester.Name,
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                })
                .ToListAsync();

            return Result<ModuleDto>.Success(new ModuleDto
            {
                Id = module.Id,
                Name = module.Name,
                ModuleCode = module.ModuleCode,
                TeacherName = module.TeacherName,
                SemesterId = module.SemesterId,
                SemesterName = semesterName,
                AttendanceRate = rate,
                TotalValidSessions = totalValid,
                PresentSessions = present,
                Schedules = schedulesList,
                CreatedAt = module.CreatedAt,
                UpdatedAt = module.UpdatedAt
            }, "Module updated successfully.");
        }
        catch (Exception ex)
        {
            return Result<ModuleDto>.Failure($"Failed to update module: {ex.Message}");
        }
    }

    public async Task<Result> DeleteModuleAsync(long id)
    {
        try
        {
            var module = await _context.TblModules.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (module == null)
            {
                return Result.Failure("Module not found.");
            }

            module.IsDeleted = true;

            // Cascade soft-delete associated recurring schedules
            var schedules = await _context.TblRecurringSchedules
                .Where(s => s.ModuleId == id && !s.IsDeleted)
                .ToListAsync();

            foreach (var schedule in schedules)
            {
                schedule.IsDeleted = true;
            }

            // Cascade soft-delete associated class sessions
            var sessions = await _context.TblSessions
                .Where(s => s.ModuleId == id && !s.IsDeleted)
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.IsDeleted = true;

                if (!string.IsNullOrEmpty(session.GoogleEventId))
                {
                    await _googleCalendarService.DeleteEventAsync(session.GoogleEventId);
                }
            }

            await _context.SaveChangesAsync();

            return Result.Success("Module deleted successfully.");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete module: {ex.Message}");
        }
    }
}
