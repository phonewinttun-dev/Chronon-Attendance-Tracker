using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.Module;
using ACST.Shared;
using Microsoft.EntityFrameworkCore;

namespace ACST.Domain.Features.Modules;

public class ModuleService : IModuleService
{
    private readonly AppDbContext _context;

    public ModuleService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ModuleDto>> GetAllModulesAsync(string? searchTerm = null, int? pageNumber = null, int? pageSize = null, long? semesterId = null)
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

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(m => EF.Functions.ToTsVector("english", m.Name + " " + (m.TeacherName ?? ""))
                    .Matches(searchTerm));
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
                        Id = m.Id,
                        Name = m.Name,
                        TeacherName = m.TeacherName,
                        SemesterId = m.SemesterId,
                        SemesterName = m.Semester != null ? m.Semester.Name : null,
                        TotalValidSessions = m.TblSessions.Count(s => !s.IsDeleted && s.Status != "Holiday" && s.Status != "Cancelled"),
                        PresentSessions = m.TblSessions.Count(s => !s.IsDeleted && s.Status == "Present"),
                        CreatedAt = m.CreatedAt,
                        UpdatedAt = m.UpdatedAt
                    })
                    .Skip((pageNumber.Value - 1) * pageSize.Value)
                    .Take(pageSize.Value)
                    .ToListAsync();

                items = rawItems.Select(m => new ModuleDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    TeacherName = m.TeacherName,
                    SemesterId = m.SemesterId,
                    SemesterName = m.SemesterName,
                    TotalValidSessions = m.TotalValidSessions,
                    PresentSessions = m.PresentSessions,
                    AttendanceRate = m.TotalValidSessions > 0 ? Math.Round((double)m.PresentSessions / m.TotalValidSessions * 100, 2) : 0,
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
                        Id = m.Id,
                        Name = m.Name,
                        TeacherName = m.TeacherName,
                        SemesterId = m.SemesterId,
                        SemesterName = m.Semester != null ? m.Semester.Name : null,
                        TotalValidSessions = m.TblSessions.Count(s => !s.IsDeleted && s.Status != "Holiday" && s.Status != "Cancelled"),
                        PresentSessions = m.TblSessions.Count(s => !s.IsDeleted && s.Status == "Present"),
                        CreatedAt = m.CreatedAt,
                        UpdatedAt = m.UpdatedAt
                    })
                    .ToListAsync();

                items = rawItems.Select(m => new ModuleDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    TeacherName = m.TeacherName,
                    SemesterId = m.SemesterId,
                    SemesterName = m.SemesterName,
                    TotalValidSessions = m.TotalValidSessions,
                    PresentSessions = m.PresentSessions,
                    AttendanceRate = m.TotalValidSessions > 0 ? Math.Round((double)m.PresentSessions / m.TotalValidSessions * 100, 2) : 0,
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
                    m.TeacherName,
                    m.SemesterId,
                    SemesterName = m.Semester != null ? m.Semester.Name : null,
                    TotalValidSessions = m.TblSessions.Count(s => !s.IsDeleted && s.Status != "Holiday" && s.Status != "Cancelled"),
                    PresentSessions = m.TblSessions.Count(s => !s.IsDeleted && s.Status == "Present"),
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
                TeacherName = moduleData.TeacherName,
                SemesterId = moduleData.SemesterId,
                SemesterName = moduleData.SemesterName,
                TotalValidSessions = moduleData.TotalValidSessions,
                PresentSessions = moduleData.PresentSessions,
                AttendanceRate = moduleData.TotalValidSessions > 0 ? Math.Round((double)moduleData.PresentSessions / moduleData.TotalValidSessions * 100, 2) : 0,
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
                TeacherName = request.TeacherName,
                SemesterId = request.SemesterId,
                IsDeleted = false
            };

            _context.TblModules.Add(module);
            await _context.SaveChangesAsync();

            return Result<ModuleDto>.Success(new ModuleDto
            {
                Id = module.Id,
                Name = module.Name,
                TeacherName = module.TeacherName,
                SemesterId = module.SemesterId,
                SemesterName = semesterName,
                AttendanceRate = 0,
                TotalValidSessions = 0,
                PresentSessions = 0,
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
            module.TeacherName = request.TeacherName;
            module.SemesterId = request.SemesterId;

            _context.TblModules.Update(module);
            await _context.SaveChangesAsync();

            var totalValid = await _context.TblSessions.CountAsync(s => s.ModuleId == id && !s.IsDeleted && s.Status != "Holiday" && s.Status != "Cancelled");
            var present = await _context.TblSessions.CountAsync(s => s.ModuleId == id && !s.IsDeleted && s.Status == "Present");
            var rate = totalValid > 0 ? Math.Round((double)present / totalValid * 100, 2) : 0;

            return Result<ModuleDto>.Success(new ModuleDto
            {
                Id = module.Id,
                Name = module.Name,
                TeacherName = module.TeacherName,
                SemesterId = module.SemesterId,
                SemesterName = semesterName,
                AttendanceRate = rate,
                TotalValidSessions = totalValid,
                PresentSessions = present,
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
            _context.TblModules.Update(module);
            await _context.SaveChangesAsync();

            return Result.Success("Module deleted successfully.");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete module: {ex.Message}");
        }
    }
}
