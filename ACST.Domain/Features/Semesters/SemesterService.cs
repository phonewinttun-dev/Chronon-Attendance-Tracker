using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.Module;
using ACST.Domain.DTOs.Semester;
using ACST.Domain.Features.GoogleCalendar;
using ACST.Shared;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ACST.Domain.Features.Semesters;

public class SemesterService : ISemesterService
{
    private readonly AppDbContext _context;
    private readonly IGoogleCalendarService _googleCalendarService;

    public SemesterService(AppDbContext context, IGoogleCalendarService googleCalendarService)
    {
        _context = context;
        _googleCalendarService = googleCalendarService;
    }

    private IQueryable<TblSemester> ActiveSemesterQuery => _context.TblSemesters
                .AsNoTracking()
                .Where(s => !s.IsDeleted);

    #region Get All Semesters
    public async Task<PagedResult<SemesterDto>> GetAllSemestersAsync(PaginationRequest request)
    {
        if (request is null)
        {
            return PagedResult<SemesterDto>.Failure("Pagination request cannot be null.");
        }

        try
        {
            var query = ActiveSemesterQuery;
            
            query = query.OrderByDescending(s => s.StartDate);

            int totalCount = await query.CountAsync();
            List<SemesterDto> items;
            Pagination pagination;

                items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(s => new SemesterDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        StartDate = s.StartDate,
                        EndDate = s.EndDate,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt
                    })
                    .ToListAsync();

                pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);

            return PagedResult<SemesterDto>.Success(items, pagination);
        }
        catch (Exception ex)
        {
            return PagedResult<SemesterDto>.Failure($"Failed to retrieve semesters: {ex.Message}");
        }
    }
    #endregion

    #region Get Semester By Id
    public async Task<Result<SemesterDto>> GetSemesterByIdAsync(long id)
    {
        try
        {
            var semester = await ActiveSemesterQuery.FirstOrDefaultAsync(s => s.Id == id);

            if (semester is null)
            {
                return Result<SemesterDto>.Failure("Semester not found.");
            }
                
            return Result<SemesterDto>.Success(new SemesterDto
            {
                Id = semester.Id,
                Name = semester.Name,
                StartDate = semester.StartDate,
                EndDate = semester.EndDate,
                CreatedAt = semester.CreatedAt,
                UpdatedAt = semester.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            return Result<SemesterDto>.Failure($"Error retrieving semester: {ex.Message}");
        }
    }
    #endregion

    #region Create Semester 
    public async Task<Result<SemesterDto>> CreateSemesterAsync(CreateSemesterRequest request)
    {
        try
        {
            if (request.StartDate > request.EndDate)
            {
                return Result<SemesterDto>.Failure("Start date cannot be after end date.");
            }
                
            var semester = new TblSemester
            {
                Name = request.Name,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsDeleted = false
            };

            _context.TblSemesters.Add(semester);
            await _context.SaveChangesAsync();

            return Result<SemesterDto>.Success(new SemesterDto
            {
                Id = semester.Id,
                Name = semester.Name,
                StartDate = semester.StartDate,
                EndDate = semester.EndDate,
                CreatedAt = semester.CreatedAt,
                UpdatedAt = semester.UpdatedAt
            },
            "Semester created successfully.");
        }
        catch (Exception ex)
        {
            return Result<SemesterDto>.Failure($"Failed to create semester: {ex.Message}");
        }
    }
    #endregion

    #region Update Semester
    public async Task<Result<SemesterDto>> UpdateSemesterAsync(long id, UpdateSemesterRequest request)
    {
        try
        {
            if (request.StartDate > request.EndDate)
            {
                return Result<SemesterDto>.Failure("Start date cannot be after end date.");
            }

            var semester = await ActiveSemesterQuery.FirstOrDefaultAsync(s => s.Id == id);

            if (semester is null)
            {
                return Result<SemesterDto>.Failure("Semester not found.");
            }

            if (!string.IsNullOrEmpty(request.Name))
            {
                semester.Name = request.Name;
            }

            if (request.StartDate != default(DateOnly))
            {
                semester.StartDate = request.StartDate;
            }

            if (request.EndDate != default(DateOnly))
            {
                semester.EndDate = request.EndDate;
            }

            await _context.SaveChangesAsync();

            return Result<SemesterDto>.Success(new SemesterDto
            {
                Id = semester.Id,
                Name = semester.Name,
                StartDate = semester.StartDate,
                EndDate = semester.EndDate,
                CreatedAt = semester.CreatedAt,
                UpdatedAt = semester.UpdatedAt
            }, "Semester updated successfully.");
        }
        catch (Exception ex)
        {
            return Result<SemesterDto>.Failure($"Failed to update semester: {ex.Message}");
        }
    }
    #endregion

    #region Delete Semester
    public async Task<Result> DeleteSemesterAsync(long id)
    {
        try
        {
            var semester = await ActiveSemesterQuery.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (semester == null)
            {
                return Result.Failure("Semester not found.");
            }

            semester.IsDeleted = true;

            // Cascade soft-delete modules belonging to this semester
            var modules = await _context.TblModules
                .Where(m => m.SemesterId == id && !m.IsDeleted)
                .ToListAsync();

            foreach (var module in modules)
            {
                module.IsDeleted = true;
            }

            // Cascade soft-delete recurring schedules belonging to this semester
            var schedules = await _context.TblRecurringSchedules
                .Where(s => s.SemesterId == id && !s.IsDeleted)
                .ToListAsync();

            foreach (var schedule in schedules)
            {
                schedule.IsDeleted = true;
            }

            // Cascade soft-delete sessions belonging to this semester and remove their Google Calendar events
            var sessions = await _context.TblSessions
                .Where(s => s.SemesterId == id && !s.IsDeleted)
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

            return Result.Success("Semester and all its associated modules, schedules, and sessions deleted successfully.");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete semester: {ex.Message}");
        }
    }
    #endregion
}
