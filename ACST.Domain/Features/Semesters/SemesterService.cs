using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.Semester;
using ACST.Shared;
using Microsoft.EntityFrameworkCore;

namespace ACST.Domain.Features.Semesters;

public class SemesterService : ISemesterService
{
    private readonly AppDbContext _context;

    public SemesterService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IEnumerable<SemesterDto>>> GetAllSemestersAsync()
    {
        try
        {
            var semesters = await _context.TblSemesters
                .AsNoTracking()
                .Where(s => !s.IsDeleted)
                .OrderByDescending(s => s.StartDate)
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

            return Result<IEnumerable<SemesterDto>>.Success(semesters);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<SemesterDto>>.Failure($"Failed to retrieve semesters: {ex.Message}");
        }
    }

    public async Task<Result<SemesterDto>> GetSemesterByIdAsync(long id)
    {
        try
        {
            var semester = await _context.TblSemesters
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (semester == null)
                return Result<SemesterDto>.Failure("Semester not found.");

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

    public async Task<Result<SemesterDto>> CreateSemesterAsync(CreateSemesterRequest request)
    {
        try
        {
            if (request.StartDate > request.EndDate)
                return Result<SemesterDto>.Failure("Start date cannot be after end date.");

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
            }, "Semester created successfully.");
        }
        catch (Exception ex)
        {
            return Result<SemesterDto>.Failure($"Failed to create semester: {ex.Message}");
        }
    }

    public async Task<Result<SemesterDto>> UpdateSemesterAsync(long id, UpdateSemesterRequest request)
    {
        try
        {
            if (request.StartDate > request.EndDate)
                return Result<SemesterDto>.Failure("Start date cannot be after end date.");

            var semester = await _context.TblSemesters.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (semester == null)
                return Result<SemesterDto>.Failure("Semester not found.");

            semester.Name = request.Name;
            semester.StartDate = request.StartDate;
            semester.EndDate = request.EndDate;

            _context.TblSemesters.Update(semester);
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

    public async Task<Result> DeleteSemesterAsync(long id)
    {
        try
        {
            var semester = await _context.TblSemesters.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (semester == null)
                return Result.Failure("Semester not found.");

            semester.IsDeleted = true;
            _context.TblSemesters.Update(semester);
            await _context.SaveChangesAsync();

            return Result.Success("Semester deleted successfully.");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete semester: {ex.Message}");
        }
    }
}
