using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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

    #region Get Overall Analytics for attendance 
    public async Task<Result<OverallAnalyticsDto>> GetOverallAnalyticsAsync(long semesterId)
    {
        try
        {
            var sessions = await _context.TblSessions
                .AsNoTracking()
                .Where(s => s.SemesterId == semesterId && !s.IsDeleted && (s.Module == null || !s.Module.IsDeleted) && (s.Semester == null || !s.Semester.IsDeleted))
                .ToListAsync();

            if (!sessions.Any())
            {
                return Result<OverallAnalyticsDto>.Failure("No sessions found for this semester.");
            }

            var stats = CalculateStats(sessions);

            return Result<OverallAnalyticsDto>.Success(new OverallAnalyticsDto
            {
                OverallRate = Math.Round(stats.AttendanceRate, 2),
                TotalPresent = stats.Present,
                TotalAbsent = stats.Absent,
                TotalSessions = stats.Valid,
                ExcludedHolidaysCount = stats.Holiday,
                ExcludedCancelledCount = stats.Cancelled
            });
        }
        catch (Exception ex)
        {
            return Result<OverallAnalyticsDto>.Failure($"Failed to get overall analytics: {ex.Message}");
        }
    }
    #endregion

    #region Get Module Analytics
    public async Task<Result<ModuleAnalyticsDto>> GetModuleAnalyticsAsync(long moduleId, long semesterId)
    {
        try
        {
            var module = await _context.TblModules.FindAsync(moduleId);
            if (module == null) return Result<ModuleAnalyticsDto>.Failure("Module not found.");

            var sessions = await _context.TblSessions
                .AsNoTracking()
                .Where(s => s.ModuleId == moduleId && s.SemesterId == semesterId && !s.IsDeleted && (s.Module == null || !s.Module.IsDeleted) && (s.Semester == null || !s.Semester.IsDeleted))
                .ToListAsync();

            var stats = CalculateStats(sessions);

            return Result<ModuleAnalyticsDto>.Success(new ModuleAnalyticsDto
            {
                ModuleId = moduleId,
                ModuleName = module.Name,
                AttendanceRate = Math.Round(stats.AttendanceRate, 2),
                TotalPresent = stats.Present,
                TotalAbsent = stats.Absent,
                TotalSessions = stats.Valid
            });
        }
        catch (Exception ex)
        {
            return Result<ModuleAnalyticsDto>.Failure($"Failed to get module analytics: {ex.Message}");
        }
    }

    #endregion

    #region Get Dashboard Summary
    public async Task<Result<DashboardSummaryDto>> GetDashboardSummaryAsync(long semesterId)
    {
        try
        {
            var cache = await _context.TblSemesterDashboardSummaries
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.SemesterId == semesterId);

            if (cache == null)
            {
                await UpdateSemesterDashboardSummaryAsync(semesterId);
                cache = await _context.TblSemesterDashboardSummaries
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.SemesterId == semesterId);

                if (cache == null)
                {
                    return Result<DashboardSummaryDto>.Failure("Semester not found or failed to calculate summary.");
                }
            }

            var warnings = new List<string>();
            try
            {
                warnings = JsonSerializer.Deserialize<List<string>>(cache.WarningsJson) ?? new List<string>();
            }
            catch
            {
                // Fallback silently if deserialization fails
            }

            return Result<DashboardSummaryDto>.Success(new DashboardSummaryDto
            {
                TodaySessionsCount = cache.TodaySessionsCount,
                UpcomingSessionsCount = cache.UpcomingSessionsCount,
                SemesterHealthRate = cache.SemesterHealthRate,
                Warnings = warnings,
                
                SemesterName = cache.SemesterName,
                StartDate = cache.StartDate,
                EndDate = cache.EndDate,
                
                TotalSessions = cache.TotalSessions,
                PresentSessions = cache.PresentSessions,
                AbsentSessions = cache.AbsentSessions,
                LateSessions = cache.LateSessions,
                CancelledSessions = cache.CancelledSessions,
                HolidaySessions = cache.HolidaySessions,
                ValidSessions = cache.ValidSessions,
                CalculatedRate = cache.CalculatedRate,
                
                DailyAttendance = new(),
                WeeklyAttendance = new(),
                MonthlyAttendance = new(),
                ModuleAttendance = new()
            });
        }
        catch (Exception ex)
        {
            return Result<DashboardSummaryDto>.Failure($"Failed to get dashboard summary: {ex.Message}");
        }
    }

    public async Task<Result<DashboardDailyWeeklyDto>> GetDashboardDailyWeeklyAsync(long semesterId)
    {
        try
        {
            var sessions = await _context.TblSessions
                .AsNoTracking()
                .Where(s => s.SemesterId == semesterId && !s.IsDeleted && (s.Module == null || !s.Module.IsDeleted) && (s.Semester == null || !s.Semester.IsDeleted))
                .ToListAsync();

            // Group by Day of Week
            var dailyBreakdown = new List<DailyAttendanceDto>();
            var daysOfWeek = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
            foreach (var day in daysOfWeek)
            {
                var daySessions = sessions.Where(s => s.SessionDate.DayOfWeek == day).ToList();
                if (daySessions.Any())
                {
                    var dStats = CalculateStats(daySessions);

                    dailyBreakdown.Add(new DailyAttendanceDto
                    {
                        DayOfWeek = day.ToString(),
                        TotalSessions = dStats.Total,
                        Present = dStats.Present,
                        Absent = dStats.Absent,
                        Late = 0,
                        Cancelled = dStats.Cancelled,
                        Holiday = dStats.Holiday,
                        ValidSessions = dStats.Valid,
                        AttendanceRate = Math.Round(dStats.AttendanceRate, 2)
                    });
                }
            }

            // Group by Weekly (Monday-Sunday calendar weeks)
            DateOnly GetMondayOfWeek(DateOnly date)
            {
                int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
                return date.AddDays(-diff);
            }

            var sessionsByWeek = sessions
                .GroupBy(s => GetMondayOfWeek(s.SessionDate))
                .OrderBy(g => g.Key)
                .ToList();

            var weeklyBreakdown = new List<WeeklyAttendanceDto>();
            int weekNum = 1;
            foreach (var group in sessionsByWeek)
            {
                var wStart = group.Key;
                var wEnd = wStart.AddDays(6);
                var wSessions = group.ToList();

                var wStats = CalculateStats(wSessions);

                weeklyBreakdown.Add(new WeeklyAttendanceDto
                {
                    WeekNumber = weekNum++,
                    WeekStartDate = wStart,
                    WeekEndDate = wEnd,
                    TotalSessions = wStats.Total,
                    Present = wStats.Present,
                    Absent = wStats.Absent,
                    Late = 0,
                    Cancelled = wStats.Cancelled,
                    Holiday = wStats.Holiday,
                    ValidSessions = wStats.Valid,
                    AttendanceRate = Math.Round(wStats.AttendanceRate, 2)
                });
            }

            // Group by Monthly
            var sessionsByMonth = sessions
                .GroupBy(s => new { s.SessionDate.Year, s.SessionDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .ToList();

            var monthlyBreakdown = new List<MonthlyAttendanceDto>();
            foreach (var group in sessionsByMonth)
            {
                var mSessions = group.ToList();
                var mStats = CalculateStats(mSessions);

                var monthName = new DateTime(group.Key.Year, group.Key.Month, 1).ToString("MMMM yyyy");

                monthlyBreakdown.Add(new MonthlyAttendanceDto
                {
                    MonthName = monthName,
                    Year = group.Key.Year,
                    Month = group.Key.Month,
                    TotalSessions = mStats.Total,
                    Present = mStats.Present,
                    Absent = mStats.Absent,
                    Late = 0,
                    Cancelled = mStats.Cancelled,
                    Holiday = mStats.Holiday,
                    ValidSessions = mStats.Valid,
                    AttendanceRate = Math.Round(mStats.AttendanceRate, 2)
                });
            }

            return Result<DashboardDailyWeeklyDto>.Success(new DashboardDailyWeeklyDto
            {
                DailyAttendance = dailyBreakdown,
                WeeklyAttendance = weeklyBreakdown,
                MonthlyAttendance = monthlyBreakdown
            });
        }
        catch (Exception ex)
        {
            return Result<DashboardDailyWeeklyDto>.Failure($"Failed to get daily/weekly dashboard summary: {ex.Message}");
        }
    }

    public async Task<Result<List<ModuleAnalyticsDto>>> GetDashboardModulesAsync(long semesterId)
    {
        try
        {
            var sessions = await _context.TblSessions
                .AsNoTracking()
                .Where(s => s.SemesterId == semesterId && !s.IsDeleted && (s.Module == null || !s.Module.IsDeleted) && (s.Semester == null || !s.Semester.IsDeleted))
                .ToListAsync();

            var modules = await _context.TblModules
                .AsNoTracking()
                .Where(m => m.SemesterId == semesterId && !m.IsDeleted)
                .ToListAsync();

            var moduleBreakdown = new List<ModuleAnalyticsDto>();
            foreach (var mod in modules)
            {
                var modSessions = sessions.Where(s => s.ModuleId == mod.Id).ToList();
                var mStats = CalculateStats(modSessions);

                moduleBreakdown.Add(new ModuleAnalyticsDto
                {
                    ModuleId = mod.Id,
                    ModuleName = mod.Name,
                    AttendanceRate = Math.Round(mStats.AttendanceRate, 2),
                    TotalPresent = mStats.Present,
                    TotalAbsent = mStats.Absent,
                    TotalLate = 0,
                    TotalSessions = mStats.Valid
                });
            }

            return Result<List<ModuleAnalyticsDto>>.Success(moduleBreakdown);
        }
        catch (Exception ex)
        {
            return Result<List<ModuleAnalyticsDto>>.Failure($"Failed to get module dashboard breakdown: {ex.Message}");
        }
    }
    #endregion

    public async Task UpdateSemesterDashboardSummaryAsync(long semesterId)
    {
        var semester = await _context.TblSemesters
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == semesterId && !s.IsDeleted);

        if (semester == null) return;

        var sessions = await _context.TblSessions
            .AsNoTracking()
            .Where(s => s.SemesterId == semesterId && !s.IsDeleted && (s.Module == null || !s.Module.IsDeleted) && (s.Semester == null || !s.Semester.IsDeleted))
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(6.5)); // Myanmar Time

        int todaySessions = sessions.Count(s => s.SessionDate == today);
        int upcomingSessions = sessions.Count(s => s.SessionDate > today && s.SessionDate <= today.AddDays(7));

        var overallStats = CalculateStats(sessions);
        double healthRate = Math.Round(overallStats.AttendanceRate, 2);

        var warnings = new List<string>();
        if (healthRate < 60)
        {
            warnings.Add("Critical: Overall attendance is below 60%.");
        }
        else if (healthRate < 75)
        {
            warnings.Add("Warning: Overall attendance is below 75%.");
        }

        var summary = await _context.TblSemesterDashboardSummaries
            .FirstOrDefaultAsync(s => s.SemesterId == semesterId);

        if (summary == null)
        {
            summary = new TblSemesterDashboardSummary { SemesterId = semesterId };
            _context.TblSemesterDashboardSummaries.Add(summary);
        }

        summary.SemesterName = semester.Name;
        summary.StartDate = semester.StartDate;
        summary.EndDate = semester.EndDate;
        summary.SemesterHealthRate = healthRate;
        summary.TodaySessionsCount = todaySessions;
        summary.UpcomingSessionsCount = upcomingSessions;
        summary.TotalSessions = overallStats.Total;
        summary.PresentSessions = overallStats.Present;
        summary.AbsentSessions = overallStats.Absent;
        summary.LateSessions = 0;
        summary.CancelledSessions = overallStats.Cancelled;
        summary.HolidaySessions = overallStats.Holiday;
        summary.ValidSessions = overallStats.Valid;
        summary.CalculatedRate = healthRate;
        summary.WarningsJson = JsonSerializer.Serialize(warnings);
        summary.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task UpdateAllActiveSemesterSummariesAsync()
    {
        var activeSemesters = await _context.TblSemesters
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .Select(s => s.Id)
            .ToListAsync();

        foreach (var semesterId in activeSemesters)
        {
            await UpdateSemesterDashboardSummaryAsync(semesterId);
        }
    }

    private record SessionStats(int Total, int Present, int Absent, int Cancelled, int Holiday)
    {
        public int Valid => Total - (Cancelled + Holiday);
        public double AttendanceRate => Valid > 0 ? (double)Present / Valid * 100 : 0;
    }

    private static SessionStats CalculateStats(IEnumerable<TblSession> sessions)
    {
        var list = sessions as IList<TblSession> ?? sessions.ToList();
        return new SessionStats(
            list.Count,
            list.Count(s => s.Status == "Present"),
            list.Count(s => s.Status == "Absent"),
            list.Count(s => s.Status == "Cancelled"),
            list.Count(s => s.Status == "Holiday")
        );
    }
}
