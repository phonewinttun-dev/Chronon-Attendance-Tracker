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

    private IQueryable<TblModule> ActiveModuleQuery => _context.TblModules
            .AsNoTracking()
            .Include(m => m.Semester)
            .Where(m => !m.IsDeleted);

    #region Get All Modules
    public async Task<PagedResult<ModuleDto>> GetAllModulesAsync(PaginationRequest request, long? semesterId = null)
    {
        if (request is null)
        {
            return PagedResult<ModuleDto>.Failure("Pagination request cannot be null.");
        }

        try
        {
            var query = ActiveModuleQuery;

            if (semesterId.HasValue)
            {
                query = query.Where(m => m.SemesterId == semesterId.Value);
            }

            int totalCount = await query.CountAsync();
            if (totalCount == 0)
            {
                return PagedResult<ModuleDto>.Failure("No modules found.");
            }

            query = query.OrderBy(m => m.Name);

            var rawItems = await query.Select(m => new
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
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var items = rawItems.Select(m => new ModuleDto
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

                var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);

                return PagedResult<ModuleDto>.Success(items, pagination);
        }
        catch (Exception ex)
        {
            return PagedResult<ModuleDto>.Failure($"Failed to retrieve modules: {ex.Message}");
        }
    }
    #endregion

    #region Get module by ID
    public async Task<Result<ModuleDto>> GetModuleByIdAsync(long id)
    {
        try
        {
            var moduleData = await ActiveModuleQuery
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
    #endregion

    #region Create Module
    //public async Task<Result<ModuleDto>> CreateModuleAsync(CreateModuleRequest request)
    //{
    //    try
    //    {
    //        string? semesterName = null;
    //        if (request.SemesterId.HasValue)
    //        {
    //            var semester = await _context.TblSemesters.FirstOrDefaultAsync(s => s.Id == request.SemesterId.Value && !s.IsDeleted);
    //            if (semester == null)
    //            {
    //                return Result<ModuleDto>.Failure("Selected semester does not exist or is deleted.");
    //            }
    //            semesterName = semester.Name;
    //        }

    //        var module = new TblModule
    //        {
    //            Name = request.Name,
    //            ModuleCode = request.ModuleCode,
    //            TeacherName = request.TeacherName,
    //            SemesterId = request.SemesterId,
    //            IsDeleted = false
    //        };

    //        _context.TblModules.Add(module);
    //        await _context.SaveChangesAsync();

    //        // Save schedules if any
    //        if (request.Schedules != null && request.Schedules.Any())
    //        {
    //            if (!request.SemesterId.HasValue)
    //            {
    //                return Result<ModuleDto>.Failure("Semester is required when adding schedules.");
    //            }

    //            foreach (var schRequest in request.Schedules)
    //            {
    //                if (schRequest.StartTime >= schRequest.EndTime)
    //                {
    //                    return Result<ModuleDto>.Failure("Start time must be before end time for all schedules.");
    //                }

    //                var schedule = new TblRecurringSchedule
    //                {
    //                    ModuleId = module.Id,
    //                    SemesterId = request.SemesterId.Value,
    //                    DayOfWeek = schRequest.DayOfWeek,
    //                    StartTime = schRequest.StartTime,
    //                    EndTime = schRequest.EndTime,
    //                    IsActive = true,
    //                    IsDeleted = false
    //                };
    //                _context.TblRecurringSchedules.Add(schedule);
    //            }
    //            await _context.SaveChangesAsync();

    //            if (request.GenerateSessions)
    //            {
    //                await _classSessionService.GenerateSessionsAsync(new GenerateSessionsRequest
    //                {
    //                    SemesterId = request.SemesterId.Value,
    //                    ModuleId = module.Id,
    //                    SyncWithGoogleCalendar = request.SyncWithGoogleCalendar
    //                });
    //            }
    //        }

    //        var schedulesList = new List<RecurringScheduleDto>();
    //        if (request.Schedules != null && request.Schedules.Any() && request.SemesterId.HasValue)
    //        {
    //            schedulesList = await _context.TblRecurringSchedules
    //                .Include(s => s.Semester)
    //                .Where(s => s.ModuleId == module.Id && !s.IsDeleted)
    //                .Select(s => new RecurringScheduleDto
    //                {
    //                    Id = s.Id,
    //                    ModuleId = s.ModuleId,
    //                    ModuleName = module.Name,
    //                    SemesterId = s.SemesterId,
    //                    SemesterName = s.Semester.Name,
    //                    DayOfWeek = s.DayOfWeek,
    //                    StartTime = s.StartTime,
    //                    EndTime = s.EndTime,
    //                    IsActive = s.IsActive,
    //                    CreatedAt = s.CreatedAt,
    //                    UpdatedAt = s.UpdatedAt
    //                })
    //                .ToListAsync();
    //        }

    //        return Result<ModuleDto>.Success(new ModuleDto
    //        {
    //            Id = module.Id,
    //            Name = module.Name,
    //            ModuleCode = module.ModuleCode,
    //            TeacherName = module.TeacherName,
    //            SemesterId = module.SemesterId,
    //            SemesterName = semesterName,
    //            AttendanceRate = 0,
    //            TotalValidSessions = 0,
    //            PresentSessions = 0,
    //            Schedules = schedulesList,
    //            CreatedAt = module.CreatedAt,
    //            UpdatedAt = module.UpdatedAt
    //        }, "Module created successfully.");
    //    }
    //    catch (Exception ex)
    //    {
    //        return Result<ModuleDto>.Failure($"Failed to create module: {ex.Message}");
    //    }
    //}
    public async Task<Result<ModuleDto>> CreateModuleAsync(CreateModuleRequest request)
    {
        // Validations
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
            }
        }

        // Begin database transaction
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            string? semesterName = null;
            if (request.SemesterId.HasValue)
            {
                var semester = await _context.TblSemesters
                    .FirstOrDefaultAsync(s => s.Id == request.SemesterId.Value && !s.IsDeleted);

                if (semester == null)
                {
                    return Result<ModuleDto>.Failure("Selected semester does not exist.");
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
            await _context.SaveChangesAsync(); // Generates module.Id

            // Save schedules if any
            if (request.Schedules != null && request.Schedules.Any())
            {
                foreach (var schRequest in request.Schedules)
                {
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

            // Fetch saved schedules (removed redundant Include)
            var schedulesList = new List<RecurringScheduleDto>();
            if (request.Schedules != null && request.Schedules.Any() && request.SemesterId.HasValue)
            {
                schedulesList = await _context.TblRecurringSchedules
                    .Where(s => s.ModuleId == module.Id && !s.IsDeleted)
                    .Select(s => new RecurringScheduleDto
                    {
                        Id = s.Id,
                        ModuleId = s.ModuleId,
                        ModuleName = module.Name,
                        SemesterId = s.SemesterId,
                        SemesterName = s.Semester != null ? s.Semester.Name : null,
                        DayOfWeek = s.DayOfWeek,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        IsActive = s.IsActive,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt
                    })
                    .ToListAsync();
            }

            // Commit transaction after all steps succeed
            await transaction.CommitAsync();

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
        catch (Exception)
        {
            // Rollback all database modifications
            await transaction.RollbackAsync();

            return Result<ModuleDto>.Failure("An unexpected error occurred while creating the module.");
        }
    }
    #endregion

    #region Update Module
    //public async Task<Result<ModuleDto>> UpdateModuleAsync(long id, UpdateModuleRequest request)
    //{
    //    try
    //    {
    //        var module = await _context.TblModules.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

    //        if (module == null)
    //        {
    //            return Result<ModuleDto>.Failure("Module not found.");
    //        }

    //        string? semesterName = null;
    //        if (request.SemesterId.HasValue)
    //        {
    //            var semester = await _context.TblSemesters.FirstOrDefaultAsync(s => s.Id == request.SemesterId.Value && !s.IsDeleted);
    //            if (semester == null)
    //            {
    //                return Result<ModuleDto>.Failure("Selected semester does not exist or is deleted.");
    //            }
    //            semesterName = semester.Name;
    //        }

    //        module.Name = request.Name;
    //        module.ModuleCode = request.ModuleCode;
    //        module.TeacherName = request.TeacherName;
    //        module.SemesterId = request.SemesterId;

    //        await _context.SaveChangesAsync();

    //        // Handle schedules update
    //        if (request.Schedules != null)
    //        {
    //            if (request.Schedules.Any() && !request.SemesterId.HasValue)
    //            {
    //                return Result<ModuleDto>.Failure("Semester is required when adding schedules.");
    //            }

    //            var existingSchedules = await _context.TblRecurringSchedules
    //                .Where(s => s.ModuleId == id && !s.IsDeleted)
    //                .ToListAsync();

    //            // Determine deleted schedules
    //            var requestIds = request.Schedules.Where(s => s.Id.HasValue).Select(s => s.Id!.Value).ToList();
    //            var toDelete = existingSchedules.Where(s => !requestIds.Contains(s.Id)).ToList();
    //            foreach (var sch in toDelete)
    //            {
    //                sch.IsDeleted = true;

    //                // Cascade soft-delete future class sessions for this deleted schedule
    //                var associatedSessions = await _context.TblSessions
    //                    .Where(s => s.RecurringScheduleId == sch.Id && !s.IsDeleted && s.StartDatetime >= DateTime.UtcNow)
    //                    .ToListAsync();
    //                foreach (var s in associatedSessions)
    //                {
    //                    s.IsDeleted = true;
    //                    if (!string.IsNullOrEmpty(s.GoogleEventId))
    //                    {
    //                        await _googleCalendarService.DeleteEventAsync(s.GoogleEventId);
    //                    }
    //                }
    //            }

    //            // Add or update schedules
    //            foreach (var schReq in request.Schedules)
    //            {
    //                if (schReq.StartTime >= schReq.EndTime)
    //                {
    //                    return Result<ModuleDto>.Failure("Start time must be before end time for all schedules.");
    //                }

    //                if (schReq.Id.HasValue)
    //                {
    //                    var existing = existingSchedules.FirstOrDefault(s => s.Id == schReq.Id.Value);
    //                    if (existing != null)
    //                    {
    //                        existing.DayOfWeek = schReq.DayOfWeek;
    //                        existing.StartTime = schReq.StartTime;
    //                        existing.EndTime = schReq.EndTime;
    //                        existing.SemesterId = request.SemesterId!.Value;
    //                    }
    //                }
    //                else
    //                {
    //                    var newSch = new TblRecurringSchedule
    //                    {
    //                        ModuleId = module.Id,
    //                        SemesterId = request.SemesterId!.Value,
    //                        DayOfWeek = schReq.DayOfWeek,
    //                        StartTime = schReq.StartTime,
    //                        EndTime = schReq.EndTime,
    //                        IsActive = true,
    //                        IsDeleted = false
    //                    };
    //                    _context.TblRecurringSchedules.Add(newSch);
    //                }
    //            }
    //            await _context.SaveChangesAsync();

    //            if (request.SemesterId.HasValue)
    //            {
    //                await _classSessionService.GenerateSessionsAsync(new GenerateSessionsRequest
    //                {
    //                    SemesterId = request.SemesterId.Value,
    //                    ModuleId = id
    //                });
    //            }
    //        }

    //        var totalValid = await _context.TblSessions.CountAsync(s => s.ModuleId == id && !s.IsDeleted && s.Status != "Holiday" && s.Status != "Cancelled");
    //        var present = await _context.TblSessions.CountAsync(s => s.ModuleId == id && !s.IsDeleted && s.Status == "Present");
    //        var rate = totalValid > 0 ? Math.Round((double)present / totalValid * 100, 2) : 0;

    //        // Fetch current list of schedules to return in DTO
    //        var schedulesList = await _context.TblRecurringSchedules
    //            .Include(s => s.Semester)
    //            .Where(s => s.ModuleId == id && !s.IsDeleted)
    //            .Select(s => new RecurringScheduleDto
    //            {
    //                Id = s.Id,
    //                ModuleId = s.ModuleId,
    //                ModuleName = module.Name,
    //                SemesterId = s.SemesterId,
    //                SemesterName = s.Semester.Name,
    //                DayOfWeek = s.DayOfWeek,
    //                StartTime = s.StartTime,
    //                EndTime = s.EndTime,
    //                IsActive = s.IsActive,
    //                CreatedAt = s.CreatedAt,
    //                UpdatedAt = s.UpdatedAt
    //            })
    //            .ToListAsync();

    //        return Result<ModuleDto>.Success(new ModuleDto
    //        {
    //            Id = module.Id,
    //            Name = module.Name,
    //            ModuleCode = module.ModuleCode,
    //            TeacherName = module.TeacherName,
    //            SemesterId = module.SemesterId,
    //            SemesterName = semesterName,
    //            AttendanceRate = rate,
    //            TotalValidSessions = totalValid,
    //            PresentSessions = present,
    //            Schedules = schedulesList,
    //            CreatedAt = module.CreatedAt,
    //            UpdatedAt = module.UpdatedAt
    //        }, "Module updated successfully.");
    //    }
    //    catch (Exception ex)
    //    {
    //        return Result<ModuleDto>.Failure($"Failed to update module: {ex.Message}");
    //    }
    //}
    public async Task<Result<ModuleDto>> UpdateModuleAsync(long id, UpdateModuleRequest request)
    {
        // 1. Early Validation (Fail-Fast)
        if (request.Schedules != null)
        {
            if (request.Schedules.Any() && !request.SemesterId.HasValue)
            {
                return Result<ModuleDto>.Failure("Semester is required when adding schedules.");
            }
            foreach (var schReq in request.Schedules)
            {
                if (schReq.StartTime >= schReq.EndTime)
                {
                    return Result<ModuleDto>.Failure("Start time must be before end time for all schedules.");
                }
            }
        }
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var module = await _context.TblModules.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            if (module == null)
            {
                return Result<ModuleDto>.Failure("Module not found.");
            }
            string? semesterName = null;
            if (request.SemesterId is long semesterId)
            {
                var semester = await _context.TblSemesters.FirstOrDefaultAsync(s => s.Id == semesterId && !s.IsDeleted);
                if (semester == null)
                {
                    return Result<ModuleDto>.Failure("Selected semester does not exist or is deleted.");
                }
                semesterName = semester.Name;
            }
            // Direct field assignment for updates
            module.Name = request.Name;
            module.ModuleCode = request.ModuleCode;
            module.TeacherName = request.TeacherName;
            module.SemesterId = request.SemesterId;
            var googleEventsToDelete = new List<string>();
            if (request.Schedules != null)
            {
                var existingSchedules = await _context.TblRecurringSchedules
                    .Where(s => s.ModuleId == id && !s.IsDeleted)
                    .ToListAsync();
                var requestIds = request.Schedules
                    .Where(s => s.Id is not null)
                    .Select(s => s.Id!.Value)
                    .ToList();
                var toDelete = existingSchedules.Where(s => !requestIds.Contains(s.Id)).ToList();
                var toDeleteIds = toDelete.Select(d => d.Id).ToList();
                if (toDeleteIds.Any())
                {
                    // Soft-delete obsolete schedules
                    foreach (var sch in toDelete)
                    {
                        sch.IsDeleted = true;
                    }
                    // Batch load all associated sessions (fixes N+1 query problem)
                    var associatedSessions = await _context.TblSessions
                        .Where(s => toDeleteIds.Contains(s.RecurringScheduleId) && !s.IsDeleted && s.StartDatetime >= DateTime.UtcNow)
                        .ToListAsync();
                    foreach (var session in associatedSessions)
                    {
                        session.IsDeleted = true;
                        if (!string.IsNullOrEmpty(session.GoogleEventId))
                        {
                            googleEventsToDelete.Add(session.GoogleEventId);
                        }
                    }
                }
                // Add or update schedules
                foreach (var schReq in request.Schedules)
                {
                    if (schReq.Id is long scheduleId)
                    {
                        var existing = existingSchedules.FirstOrDefault(s => s.Id == scheduleId);
                        if (existing != null)
                        {
                            existing.DayOfWeek = schReq.DayOfWeek;
                            existing.StartTime = schReq.StartTime;
                            existing.EndTime = schReq.EndTime;
                            existing.SemesterId = request.SemesterId ?? 0;
                        }
                    }
                    else
                    {
                        var newSch = new TblRecurringSchedule
                        {
                            ModuleId = module.Id,
                            SemesterId = request.SemesterId ?? 0,
                            DayOfWeek = schReq.DayOfWeek,
                            StartTime = schReq.StartTime,
                            EndTime = schReq.EndTime,
                            IsActive = true,
                            IsDeleted = false
                        };
                        _context.TblRecurringSchedules.Add(newSch);
                    }
                }
            }
            await _context.SaveChangesAsync();
            if (request.Schedules != null && request.SemesterId is long validSemesterId)
            {
                await _classSessionService.GenerateSessionsAsync(new GenerateSessionsRequest
                {
                    SemesterId = validSemesterId,
                    ModuleId = id
                });
            }
            // Combine attendance counting into a single query
            var sessionStats = await _context.TblSessions
                .Where(s => s.ModuleId == id && !s.IsDeleted)
                .GroupBy(s => 1)
                .Select(g => new
                {
                    TotalValid = g.Count(s => s.Status != "Holiday" && s.Status != "Cancelled"),
                    Present = g.Count(s => s.Status == "Present")
                })
                .FirstOrDefaultAsync();
            var totalValid = sessionStats?.TotalValid ?? 0;
            var present = sessionStats?.Present ?? 0;
            var rate = totalValid > 0 ? Math.Round((double)present / totalValid * 100, 2) : 0;
            var schedulesList = await _context.TblRecurringSchedules
                .Where(s => s.ModuleId == id && !s.IsDeleted)
                .Select(s => new RecurringScheduleDto
                {
                    Id = s.Id,
                    ModuleId = s.ModuleId,
                    ModuleName = module.Name,
                    SemesterId = s.SemesterId,
                    SemesterName = s.Semester != null ? s.Semester.Name : null,
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                })
                .ToListAsync();
            await transaction.CommitAsync();
            // Asynchronous Out-of-Transaction Cleanup for External Services
            // Prevents holding DB locks during slow Google Calendar API calls
            foreach (var eventId in googleEventsToDelete)
            {
                try
                {
                    await _googleCalendarService.DeleteEventAsync(eventId);
                }
                catch (Exception)
                {
                    //_logger.LogWarning(calendarEx, "Failed to delete orphaned Google Calendar event {EventId}", eventId);
                }
            }
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
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return Result<ModuleDto>.Failure("Failed to update module due to an internal server error.");
        }
    }
    #endregion

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
