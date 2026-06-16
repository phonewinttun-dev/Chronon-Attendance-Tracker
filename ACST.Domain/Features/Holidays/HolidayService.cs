using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.Holiday;
using ACST.Shared;
using Microsoft.EntityFrameworkCore;

namespace ACST.Domain.Features.Holidays;

public class HolidayService : IHolidayService
{
    private readonly AppDbContext _context;

    public HolidayService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<HolidayDto>> GetAllHolidaysAsync(int? pageNumber = null, int? pageSize = null)
    {
        try
        {
            var query = _context.TblHolidays
                .AsNoTracking()
                .Where(h => !h.IsDeleted)
                .OrderByDescending(h => h.HolidayDate);

            int totalCount = await query.CountAsync();
            List<HolidayDto> items;
            Pagination pagination;

            if (pageNumber.HasValue && pageSize.HasValue)
            {
                items = await query
                    .Skip((pageNumber.Value - 1) * pageSize.Value)
                    .Take(pageSize.Value)
                    .Select(h => new HolidayDto
                    {
                        Id = h.Id,
                        Name = h.Name,
                        HolidayDate = h.HolidayDate
                    })
                    .ToListAsync();

                pagination = new Pagination(pageNumber.Value, pageSize.Value, totalCount);
            }
            else
            {
                items = await query
                    .Select(h => new HolidayDto
                    {
                        Id = h.Id,
                        Name = h.Name,
                        HolidayDate = h.HolidayDate
                    })
                    .ToListAsync();

                pagination = new Pagination(1, totalCount > 0 ? totalCount : 1, totalCount);
            }

            return PagedResult<HolidayDto>.Success(items, pagination);
        }
        catch (Exception ex)
        {
            return PagedResult<HolidayDto>.Failure($"Failed to retrieve holidays: {ex.Message}");
        }
    }

    public async Task<Result<HolidayDto>> CreateHolidayAsync(CreateHolidayRequest request)
    {
        try
        {
            var existing = await _context.TblHolidays.FirstOrDefaultAsync(h => h.HolidayDate == request.HolidayDate && !h.IsDeleted);
            if (existing != null)
                return Result<HolidayDto>.Failure($"A holiday already exists for date {request.HolidayDate}.");

            var holiday = new TblHoliday
            {
                Name = request.Name,
                HolidayDate = request.HolidayDate,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.TblHolidays.Add(holiday);
            await _context.SaveChangesAsync();

            return Result<HolidayDto>.Success(new HolidayDto
            {
                Id = holiday.Id,
                Name = holiday.Name,
                HolidayDate = holiday.HolidayDate
            }, "Holiday created successfully.");
        }
        catch (Exception ex)
        {
            return Result<HolidayDto>.Failure($"Failed to create holiday: {ex.Message}");
        }
    }

    public async Task<Result> SeedHolidaysAsync()
    {
        try
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "Features", "Holidays", "myanmar_holidays_2026.json");
            
            // Fallback for different project execution directories
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "ACST.Domain", "Features", "Holidays", "myanmar_holidays_2026.json");
            }

            if (!File.Exists(filePath))
            {
                return Result.Failure($"Seeding file not found at {filePath}. Cannot seed holidays.");
            }

            var jsonText = await File.ReadAllTextAsync(filePath);
            var seedData = JsonSerializer.Deserialize<List<SeedHolidayModel>>(jsonText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (seedData == null || !seedData.Any())
                return Result.Failure("No holidays found in seed file.");

            int addedCount = 0;
            foreach (var item in seedData)
            {
                var exists = await _context.TblHolidays.AnyAsync(h => h.HolidayDate == item.Date && !h.IsDeleted);
                if (!exists)
                {
                    _context.TblHolidays.Add(new TblHoliday
                    {
                        HolidayDate = item.Date,
                        Name = item.Name,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow
                    });
                    addedCount++;
                }
            }

            if (addedCount > 0)
                await _context.SaveChangesAsync();

            return Result.Success($"Successfully seeded {addedCount} new holidays.");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to seed holidays: {ex.Message}");
        }
    }

    public async Task<Result> DeleteHolidayAsync(long id)
    {
        try
        {
            var holiday = await _context.TblHolidays.FirstOrDefaultAsync(h => h.Id == id && !h.IsDeleted);
            if (holiday == null)
                return Result.Failure("Holiday not found.");

            holiday.IsDeleted = true;
            _context.TblHolidays.Update(holiday);
            await _context.SaveChangesAsync();

            return Result.Success("Holiday deleted successfully.");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete holiday: {ex.Message}");
        }
    }

    private class SeedHolidayModel
    {
        public DateOnly Date { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
