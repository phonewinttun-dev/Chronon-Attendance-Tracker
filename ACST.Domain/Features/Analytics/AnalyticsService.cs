using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.Analytics;
using ACST.Shared;
using Microsoft.EntityFrameworkCore;

namespace ACST.Domain.Features.Analytics;

public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _context;

    public AnalyticsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<OverallAnalyticsDto>> GetOverallAnalyticsAsync(long semesterId)
    {
        try
        {
            var sessions = await _context.TblSessions
                .AsNoTracking()
                .Where(s => s.SemesterId == semesterId && !s.IsDeleted)
                .ToListAsync();

            if (!sessions.Any())
                return Result<OverallAnalyticsDto>.Failure("No sessions found for this semester.");

            int totalHolidays = sessions.Count(s => s.Status == "Holiday");
            int totalCancelled = sessions.Count(s => s.Status == "Cancelled");
            
            var validSessions = sessions.Where(s => s.Status != "Holiday" && s.Status != "Cancelled").ToList();
            int totalValid = validSessions.Count;

            int present = validSessions.Count(s => s.Status == "Present");
            int absent = validSessions.Count(s => s.Status == "Absent");
            int late = validSessions.Count(s => s.Status == "Late");

            double rate = totalValid > 0 ? (double)present / totalValid * 100 : 0;

            return Result<OverallAnalyticsDto>.Success(new OverallAnalyticsDto
            {
                OverallRate = Math.Round(rate, 2),
                TotalPresent = present,
                TotalAbsent = absent,
                TotalLate = late,
                TotalSessions = totalValid,
                ExcludedHolidaysCount = totalHolidays,
                ExcludedCancelledCount = totalCancelled
            });
        }
        catch (Exception ex)
        {
            return Result<OverallAnalyticsDto>.Failure($"Failed to get overall analytics: {ex.Message}");
        }
    }

    public async Task<Result<ModuleAnalyticsDto>> GetModuleAnalyticsAsync(long moduleId, long semesterId)
    {
        try
        {
            var module = await _context.TblModules.FindAsync(moduleId);
            if (module == null) return Result<ModuleAnalyticsDto>.Failure("Module not found.");

            var sessions = await _context.TblSessions
                .AsNoTracking()
                .Where(s => s.ModuleId == moduleId && s.SemesterId == semesterId && !s.IsDeleted)
                .ToListAsync();

            var validSessions = sessions.Where(s => s.Status != "Holiday" && s.Status != "Cancelled").ToList();
            int totalValid = validSessions.Count;

            int present = validSessions.Count(s => s.Status == "Present");
            int absent = validSessions.Count(s => s.Status == "Absent");
            int late = validSessions.Count(s => s.Status == "Late");

            double rate = totalValid > 0 ? (double)present / totalValid * 100 : 0;

            return Result<ModuleAnalyticsDto>.Success(new ModuleAnalyticsDto
            {
                ModuleId = moduleId,
                ModuleName = module.Name,
                AttendanceRate = Math.Round(rate, 2),
                TotalPresent = present,
                TotalAbsent = absent,
                TotalLate = late,
                TotalSessions = totalValid
            });
        }
        catch (Exception ex)
        {
            return Result<ModuleAnalyticsDto>.Failure($"Failed to get module analytics: {ex.Message}");
        }
    }

    public async Task<Result<DashboardSummaryDto>> GetDashboardSummaryAsync(long semesterId)
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(6.5)); // Myanmar Time

            var todaySessions = await _context.TblSessions
                .AsNoTracking()
                .CountAsync(s => s.SemesterId == semesterId && s.SessionDate == today && !s.IsDeleted);

            var upcomingSessions = await _context.TblSessions
                .AsNoTracking()
                .CountAsync(s => s.SemesterId == semesterId && s.SessionDate > today && s.SessionDate <= today.AddDays(7) && !s.IsDeleted);

            var overallResult = await GetOverallAnalyticsAsync(semesterId);
            double healthRate = overallResult.IsSuccess && overallResult.Data != null ? overallResult.Data.OverallRate : 0;

            var warnings = new List<string>();
            if (healthRate < 60)
            {
                warnings.Add("Critical: Overall attendance is below 60%.");
            }
            else if (healthRate < 75)
            {
                warnings.Add("Warning: Overall attendance is below 75%.");
            }

            return Result<DashboardSummaryDto>.Success(new DashboardSummaryDto
            {
                TodaySessionsCount = todaySessions,
                UpcomingSessionsCount = upcomingSessions,
                SemesterHealthRate = healthRate,
                Warnings = warnings
            });
        }
        catch (Exception ex)
        {
            return Result<DashboardSummaryDto>.Failure($"Failed to get dashboard summary: {ex.Message}");
        }
    }
}
