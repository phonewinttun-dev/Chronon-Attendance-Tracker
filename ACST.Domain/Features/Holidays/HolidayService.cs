using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.Holiday;
using ACST.Domain.Features.GoogleCalendar;
using ACST.Shared;
using Microsoft.EntityFrameworkCore;

namespace ACST.Domain.Features.Holidays;

public class HolidayService : IHolidayService
{
    private readonly AppDbContext _context;
    private readonly IGoogleCalendarService _calendarService;

    public HolidayService(AppDbContext context, IGoogleCalendarService calendarService)
    {
        _context = context;
        _calendarService = calendarService;
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



    public async Task<Result> DeleteHolidayAsync(long id)
    {
        try
        {
            var holiday = await _context.TblHolidays.FirstOrDefaultAsync(h => h.Id == id && !h.IsDeleted);
            if (holiday == null)
                return Result.Failure("Holiday not found.");

            holiday.IsDeleted = true;
            await _context.SaveChangesAsync();

            return Result.Success("Holiday deleted successfully.");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete holiday: {ex.Message}");
        }
    }

    public async Task<Result<int>> ImportGoogleCalendarHolidaysAsync(string calendarId, DateOnly startDate, DateOnly endDate)
    {
        try
        {
            var startUtc = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var endUtc = endDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

            var fetchResult = await _calendarService.FetchHolidaysAsync(calendarId, startUtc, endUtc);
            if (!fetchResult.IsSuccess)
            {
                return Result<int>.Failure($"Failed to fetch holidays from Google Calendar: {fetchResult.Message}");
            }

            var fetchedHolidays = fetchResult.Data;
            if (fetchedHolidays == null || !fetchedHolidays.Any())
            {
                return Result<int>.Success(0, "No holidays found in the specified calendar and date range.");
            }

            int addedCount = 0;
            var existingHolidays = await _context.TblHolidays
                .Where(h => !h.IsDeleted && h.HolidayDate >= startDate && h.HolidayDate <= endDate)
                .Select(h => h.HolidayDate)
                .ToListAsync();

            var existingDates = new HashSet<DateOnly>(existingHolidays);

            foreach (var holiday in fetchedHolidays)
            {
                if (!existingDates.Contains(holiday.HolidayDate))
                {
                    _context.TblHolidays.Add(new TblHoliday
                    {
                        Name = holiday.Name,
                        HolidayDate = holiday.HolidayDate,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow
                    });

                    existingDates.Add(holiday.HolidayDate);
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return Result<int>.Success(addedCount, $"Successfully imported {addedCount} new holidays.");
        }
        catch (Exception ex)
        {
            return Result<int>.Failure($"Failed to import holidays: {ex.Message}");
        }
    }

}
