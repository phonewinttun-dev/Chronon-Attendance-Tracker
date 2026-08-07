using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.Analytics;
using ACST.Shared;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ACST.Domain.Features.Analytics;

public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AnalyticsService(AppDbContext context, IHttpContextAccessor? httpContextAccessor = null)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    private int? CurrentUserId
    {
        get
        {
            var claim = _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var userId) ? userId : null;
        }
    }

    #region Get Overall Analytics for attendance 
    public async Task<Result<OverallAnalyticsDto>> GetOverallAnalyticsAsync(long semesterId)
    {
        try
        {
            var stats = await _context.TblSessions
                .AsNoTracking()
                .Where(s => s.SemesterId == semesterId && !s.IsDeleted && (s.Module == null || !s.Module.IsDeleted) && (s.Semester == null || !s.Semester.IsDeleted))
                .GroupBy(s => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Present = g.Count(s => s.Status == "Present"),
                    Absent = g.Count(s => s.Status == "Absent"),
                    Cancelled = g.Count(s => s.Status == "Cancelled"),
                    Holiday = g.Count(s => s.Status == "Holiday"),
                    NotMarked = g.Count(s => s.Status == "Not Marked")
                })
                .FirstOrDefaultAsync();

            if (stats == null || stats.Total == 0)
            {
                return Result<OverallAnalyticsDto>.Failure("No sessions found for this semester.");
            }

            int valid = stats.Total - (stats.Cancelled + stats.Holiday + stats.NotMarked);
            double attendanceRate = valid > 0 ? (double)stats.Present / valid * 100 : 0;

            return Result<OverallAnalyticsDto>.Success(new OverallAnalyticsDto
            {
                OverallRate = Math.Round(attendanceRate, 2),
                TotalPresent = stats.Present,
                TotalAbsent = stats.Absent,
                TotalSessions = valid,
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

            var stats = await _context.TblSessions
                .AsNoTracking()
                .Where(s => s.ModuleId == moduleId && s.SemesterId == semesterId && !s.IsDeleted && (s.Module == null || !s.Module.IsDeleted) && (s.Semester == null || !s.Semester.IsDeleted))
                .GroupBy(s => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Present = g.Count(s => s.Status == "Present"),
                    Absent = g.Count(s => s.Status == "Absent"),
                    Cancelled = g.Count(s => s.Status == "Cancelled"),
                    Holiday = g.Count(s => s.Status == "Holiday"),
                    NotMarked = g.Count(s => s.Status == "Not Marked")
                })
                .FirstOrDefaultAsync();

            int total = stats?.Total ?? 0;
            int present = stats?.Present ?? 0;
            int absent = stats?.Absent ?? 0;
            int cancelled = stats?.Cancelled ?? 0;
            int holiday = stats?.Holiday ?? 0;
            int notMarked = stats?.NotMarked ?? 0;

            int valid = total - (cancelled + holiday + notMarked);
            double rate = valid > 0 ? (double)present / valid * 100 : 0;

            return Result<ModuleAnalyticsDto>.Success(new ModuleAnalyticsDto
            {
                ModuleId = moduleId,
                ModuleName = module.Name,
                AttendanceRate = Math.Round(rate, 2),
                TotalPresent = present,
                TotalAbsent = absent,
                TotalSessions = total,
                NotMarked = notMarked,
                Cancelled = cancelled,
                Holiday = holiday
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
                NotMarkedSessions = cache.TotalSessions - (cache.PresentSessions + cache.AbsentSessions + cache.CancelledSessions + cache.HolidaySessions),
                ValidSessions = cache.ValidSessions,
                CalculatedRate = cache.CalculatedRate,
                TodayAttendanceRate = cache.TodayAttendanceRate,
                
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

    private record SessionDateAggregate(DateOnly Date, int Total, int Present, int Absent, int Cancelled, int Holiday, int NotMarked);

    private static (int Total, int Present, int Absent, int Cancelled, int Holiday, int NotMarked, int Valid, double AttendanceRate) CombineAggregates(IEnumerable<SessionDateAggregate> aggs)
    {
        int total = 0, present = 0, absent = 0, cancelled = 0, holiday = 0, notMarked = 0;
        foreach (var a in aggs)
        {
            total += a.Total;
            present += a.Present;
            absent += a.Absent;
            cancelled += a.Cancelled;
            holiday += a.Holiday;
            notMarked += a.NotMarked;
        }
        int valid = total - (cancelled + holiday + notMarked);
        double rate = valid > 0 ? (double)present / valid * 100 : 0;
        return (total, present, absent, cancelled, holiday, notMarked, valid, rate);
    }

    public async Task<Result<DashboardDailyWeeklyDto>> GetDashboardDailyWeeklyAsync(long semesterId, int? month = null)
    {
        try
        {
            var query = _context.TblSessions
                .AsNoTracking()
                .Where(s => s.SemesterId == semesterId && !s.IsDeleted && (s.Module == null || !s.Module.IsDeleted) && (s.Semester == null || !s.Semester.IsDeleted));

            if (month.HasValue)
            {
                query = query.Where(s => s.SessionDate.Month == month.Value);
            }

            var dateAggregates = await query
                .GroupBy(s => s.SessionDate)
                .Select(g => new SessionDateAggregate(
                    g.Key,
                    g.Count(),
                    g.Count(s => s.Status == "Present"),
                    g.Count(s => s.Status == "Absent"),
                    g.Count(s => s.Status == "Cancelled"),
                    g.Count(s => s.Status == "Holiday"),
                    g.Count(s => s.Status == "Not Marked")
                ))
                .ToListAsync();

            // Group by Day of Week
            var dailyBreakdown = new List<DailyAttendanceDto>();
            var daysOfWeek = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
            foreach (var day in daysOfWeek)
            {
                var dayAggs = dateAggregates.Where(a => a.Date.DayOfWeek == day).ToList();
                if (dayAggs.Any())
                {
                    var dStats = CombineAggregates(dayAggs);

                    dailyBreakdown.Add(new DailyAttendanceDto
                    {
                        DayOfWeek = day.ToString(),
                        TotalSessions = dStats.Total,
                        Present = dStats.Present,
                        Absent = dStats.Absent,
                        Late = 0,
                        Cancelled = dStats.Cancelled,
                        Holiday = dStats.Holiday,
                        NotMarked = dStats.NotMarked,
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

            var aggsByWeek = dateAggregates
                .GroupBy(a => GetMondayOfWeek(a.Date))
                .OrderBy(g => g.Key)
                .ToList();

            var weeklyBreakdown = new List<WeeklyAttendanceDto>();
            int weekNum = 1;
            foreach (var group in aggsByWeek)
            {
                var wStart = group.Key;
                var wEnd = wStart.AddDays(6);
                var wStats = CombineAggregates(group);

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
                    NotMarked = wStats.NotMarked,
                    ValidSessions = wStats.Valid,
                    AttendanceRate = Math.Round(wStats.AttendanceRate, 2)
                });
            }

            // Group by Monthly
            var aggsByMonth = dateAggregates
                .GroupBy(a => new { a.Date.Year, a.Date.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .ToList();

            var monthlyBreakdown = new List<MonthlyAttendanceDto>();
            foreach (var group in aggsByMonth)
            {
                var mStats = CombineAggregates(group);
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
                    NotMarked = mStats.NotMarked,
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

    public async Task<Result<List<ModuleAnalyticsDto>>> GetDashboardModulesAsync(long semesterId, int? month = null)
    {
        try
        {
            var modules = await _context.TblModules
                .AsNoTracking()
                .Where(m => m.SemesterId == semesterId && !m.IsDeleted)
                .ToListAsync();

            var query = _context.TblSessions
                .AsNoTracking()
                .Where(s => s.SemesterId == semesterId && !s.IsDeleted && (s.Module == null || !s.Module.IsDeleted) && (s.Semester == null || !s.Semester.IsDeleted));

            if (month.HasValue)
            {
                query = query.Where(s => s.SessionDate.Month == month.Value);
            }

            var moduleAggregates = await query
                .GroupBy(s => s.ModuleId)
                .Select(g => new
                {
                    ModuleId = g.Key,
                    Total = g.Count(),
                    Present = g.Count(s => s.Status == "Present"),
                    Absent = g.Count(s => s.Status == "Absent"),
                    Cancelled = g.Count(s => s.Status == "Cancelled"),
                    Holiday = g.Count(s => s.Status == "Holiday"),
                    NotMarked = g.Count(s => s.Status == "Not Marked")
                })
                .ToDictionaryAsync(x => x.ModuleId);

            var moduleBreakdown = new List<ModuleAnalyticsDto>();
            foreach (var mod in modules)
            {
                moduleAggregates.TryGetValue(mod.Id, out var mStats);

                int total = mStats?.Total ?? 0;
                int present = mStats?.Present ?? 0;
                int absent = mStats?.Absent ?? 0;
                int cancelled = mStats?.Cancelled ?? 0;
                int holiday = mStats?.Holiday ?? 0;
                int notMarked = mStats?.NotMarked ?? 0;

                int valid = total - (cancelled + holiday + notMarked);
                double rate = valid > 0 ? (double)present / valid * 100 : 0;

                moduleBreakdown.Add(new ModuleAnalyticsDto
                {
                    ModuleId = mod.Id,
                    ModuleName = mod.Name,
                    AttendanceRate = Math.Round(rate, 2),
                    TotalPresent = present,
                    TotalAbsent = absent,
                    TotalLate = 0,
                    TotalSessions = total,
                    NotMarked = notMarked,
                    Cancelled = cancelled,
                    Holiday = holiday
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

        var baseQuery = _context.TblSessions
            .AsNoTracking()
            .Where(s => s.SemesterId == semesterId && !s.IsDeleted && (s.Module == null || !s.Module.IsDeleted) && (s.Semester == null || !s.Semester.IsDeleted));

        var overallStats = await baseQuery
            .GroupBy(s => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Present = g.Count(s => s.Status == "Present"),
                Absent = g.Count(s => s.Status == "Absent"),
                Cancelled = g.Count(s => s.Status == "Cancelled"),
                Holiday = g.Count(s => s.Status == "Holiday"),
                NotMarked = g.Count(s => s.Status == "Not Marked")
            })
            .FirstOrDefaultAsync();

        int total = overallStats?.Total ?? 0;
        int present = overallStats?.Present ?? 0;
        int absent = overallStats?.Absent ?? 0;
        int cancelled = overallStats?.Cancelled ?? 0;
        int holiday = overallStats?.Holiday ?? 0;
        int notMarked = overallStats?.NotMarked ?? 0;

        int valid = total - (cancelled + holiday + notMarked);
        double healthRate = valid > 0 ? Math.Round((double)present / valid * 100, 2) : 0;

        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(6.5)); // Myanmar Time

        var todayStats = await baseQuery
            .Where(s => s.SessionDate == today)
            .GroupBy(s => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Present = g.Count(s => s.Status == "Present"),
                Absent = g.Count(s => s.Status == "Absent"),
                Cancelled = g.Count(s => s.Status == "Cancelled"),
                Holiday = g.Count(s => s.Status == "Holiday"),
                NotMarked = g.Count(s => s.Status == "Not Marked")
            })
            .FirstOrDefaultAsync();

        int todaySessionsCount = todayStats?.Total ?? 0;
        double? todayAttendanceRate = null;
        if (todayStats != null)
        {
            int todayValid = todayStats.Total - (todayStats.Cancelled + todayStats.Holiday + todayStats.NotMarked);
            if (todayValid > 0)
            {
                todayAttendanceRate = Math.Round((double)todayStats.Present / todayValid * 100, 2);
            }
        }

        int upcomingSessionsCount = await baseQuery
            .CountAsync(s => s.SessionDate > today && s.SessionDate <= today.AddDays(7));

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
        summary.TodaySessionsCount = todaySessionsCount;
        summary.UpcomingSessionsCount = upcomingSessionsCount;
        summary.TodayAttendanceRate = todayAttendanceRate;
        summary.TotalSessions = total;
        summary.PresentSessions = present;
        summary.AbsentSessions = absent;
        summary.LateSessions = 0;
        summary.CancelledSessions = cancelled;
        summary.HolidaySessions = holiday;
        summary.ValidSessions = valid;
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

    private record SessionStats(int Total, int Present, int Absent, int Cancelled, int Holiday, int NotMarked)
    {
        public int Valid => Total - (Cancelled + Holiday + NotMarked);
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
            list.Count(s => s.Status == "Holiday"),
            list.Count(s => s.Status == "Not Marked")
        );
    }
}
