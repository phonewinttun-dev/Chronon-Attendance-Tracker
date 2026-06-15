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

    public async Task<Result<IEnumerable<ModuleDto>>> GetAllModulesAsync()
    {
        try
        {
            var modules = await _context.TblModules
                .AsNoTracking()
                .Where(m => !m.IsDeleted)
                .OrderBy(m => m.Name)
                .Select(m => new ModuleDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    TeacherName = m.TeacherName,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                })
                .ToListAsync();

            return Result<IEnumerable<ModuleDto>>.Success(modules);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<ModuleDto>>.Failure($"Failed to retrieve modules: {ex.Message}");
        }
    }

    public async Task<Result<ModuleDto>> GetModuleByIdAsync(long id)
    {
        try
        {
            var module = await _context.TblModules
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (module == null)
            {
                return Result<ModuleDto>.Failure("Module not found.");
            }

            return Result<ModuleDto>.Success(new ModuleDto
            {
                Id = module.Id,
                Name = module.Name,
                TeacherName = module.TeacherName,
                CreatedAt = module.CreatedAt,
                UpdatedAt = module.UpdatedAt
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
            var module = new TblModule
            {
                Name = request.Name,
                TeacherName = request.TeacherName,
                IsDeleted = false
            };

            _context.TblModules.Add(module);
            await _context.SaveChangesAsync();

            return Result<ModuleDto>.Success(new ModuleDto
            {
                Id = module.Id,
                Name = module.Name,
                TeacherName = module.TeacherName,
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

            module.Name = request.Name;
            module.TeacherName = request.TeacherName;

            _context.TblModules.Update(module);
            await _context.SaveChangesAsync();

            return Result<ModuleDto>.Success(new ModuleDto
            {
                Id = module.Id,
                Name = module.Name,
                TeacherName = module.TeacherName,
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
