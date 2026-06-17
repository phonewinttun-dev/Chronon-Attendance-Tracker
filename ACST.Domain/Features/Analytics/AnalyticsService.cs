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
            var semester = await _context.TblSemesters
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == semesterId && !s.IsDeleted);

            if (semester == null)
            {
                return Result<DashboardSummaryDto>.Failure("Semester not found.");
            }

            var sessions = await _context.TblSessions
                .AsNoTracking()
                .Where(s => s.SemesterId == semesterId && !s.IsDeleted)
                .ToListAsync();

            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(6.5)); // Myanmar Time

            int todaySessions = sessions.Count(s => s.SessionDate == today);
            int upcomingSessions = sessions.Count(s => s.SessionDate > today && s.SessionDate <= today.AddDays(7));

            int total = sessions.Count;
            int present = sessions.Count(s => s.Status == "Present");
            int absent = sessions.Count(s => s.Status == "Absent");
            int late = sessions.Count(s => s.Status == "Late");
            int cancelled = sessions.Count(s => s.Status == "Cancelled");
            int holiday = sessions.Count(s => s.Status == "Holiday");

            int valid = total - (cancelled + holiday);
            double healthRate = valid > 0 ? (double)present / valid * 100 : 0;
            healthRate = Math.Round(healthRate, 2);

            var warnings = new List<string>();
            if (healthRate < 60)
            {
                warnings.Add("Critical: Overall attendance is below 60%.");
            }
            else if (healthRate < 75)
            {
                warnings.Add("Warning: Overall attendance is below 75%.");
            }

            // Group by Day of Week
            var dailyBreakdown = new List<DailyAttendanceDto>();
            var daysOfWeek = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
            foreach (var day in daysOfWeek)
            {
                var daySessions = sessions.Where(s => s.SessionDate.DayOfWeek == day).ToList();
                if (daySessions.Any())
                {
                    int dTotal = daySessions.Count;
                    int dPresent = daySessions.Count(s => s.Status == "Present");
                    int dAbsent = daySessions.Count(s => s.Status == "Absent");
                    int dLate = daySessions.Count(s => s.Status == "Late");
                    int dCancelled = daySessions.Count(s => s.Status == "Cancelled");
                    int dHoliday = daySessions.Count(s => s.Status == "Holiday");
                    int dValid = dTotal - (dCancelled + dHoliday);
                    double dRate = dValid > 0 ? (double)dPresent / dValid * 100 : 0;

                    dailyBreakdown.Add(new DailyAttendanceDto
                    {
                        DayOfWeek = day.ToString(),
                        TotalSessions = dTotal,
                        Present = dPresent,
                        Absent = dAbsent,
                        Late = dLate,
                        Cancelled = dCancelled,
                        Holiday = dHoliday,
                        ValidSessions = dValid,
                        AttendanceRate = Math.Round(dRate, 2)
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

                int wTotal = wSessions.Count;
                int wPresent = wSessions.Count(s => s.Status == "Present");
                int wAbsent = wSessions.Count(s => s.Status == "Absent");
                int wLate = wSessions.Count(s => s.Status == "Late");
                int wCancelled = wSessions.Count(s => s.Status == "Cancelled");
                int wHoliday = wSessions.Count(s => s.Status == "Holiday");
                int wValid = wTotal - (wCancelled + wHoliday);
                double wRate = wValid > 0 ? (double)wPresent / wValid * 100 : 0;

                weeklyBreakdown.Add(new WeeklyAttendanceDto
                {
                    WeekNumber = weekNum++,
                    WeekStartDate = wStart,
                    WeekEndDate = wEnd,
                    TotalSessions = wTotal,
                    Present = wPresent,
                    Absent = wAbsent,
                    Late = wLate,
                    Cancelled = wCancelled,
                    Holiday = wHoliday,
                    ValidSessions = wValid,
                    AttendanceRate = Math.Round(wRate, 2)
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
                int mTotal = mSessions.Count;
                int mPresent = mSessions.Count(s => s.Status == "Present");
                int mAbsent = mSessions.Count(s => s.Status == "Absent");
                int mLate = mSessions.Count(s => s.Status == "Late");
                int mCancelled = mSessions.Count(s => s.Status == "Cancelled");
                int mHoliday = mSessions.Count(s => s.Status == "Holiday");
                int mValid = mTotal - (mCancelled + mHoliday);
                double mRate = mValid > 0 ? (double)mPresent / mValid * 100 : 0;

                var monthName = new DateTime(group.Key.Year, group.Key.Month, 1).ToString("MMMM yyyy");

                monthlyBreakdown.Add(new MonthlyAttendanceDto
                {
                    MonthName = monthName,
                    Year = group.Key.Year,
                    Month = group.Key.Month,
                    TotalSessions = mTotal,
                    Present = mPresent,
                    Absent = mAbsent,
                    Late = mLate,
                    Cancelled = mCancelled,
                    Holiday = mHoliday,
                    ValidSessions = mValid,
                    AttendanceRate = Math.Round(mRate, 2)
                });
            }

            // Group by Module
            var modules = await _context.TblModules
                .AsNoTracking()
                .Where(m => m.SemesterId == semesterId && !m.IsDeleted)
                .ToListAsync();

            var moduleBreakdown = new List<ModuleAnalyticsDto>();
            foreach (var mod in modules)
            {
                var modSessions = sessions.Where(s => s.ModuleId == mod.Id).ToList();
                var mValidSessions = modSessions.Where(s => s.Status != "Holiday" && s.Status != "Cancelled").ToList();
                int mTotalValid = mValidSessions.Count;

                int mPresent = mValidSessions.Count(s => s.Status == "Present");
                int mAbsent = mValidSessions.Count(s => s.Status == "Absent");
                int mLate = mValidSessions.Count(s => s.Status == "Late");

                double mRate = mTotalValid > 0 ? (double)mPresent / mTotalValid * 100 : 0;

                moduleBreakdown.Add(new ModuleAnalyticsDto
                {
                    ModuleId = mod.Id,
                    ModuleName = mod.Name,
                    AttendanceRate = Math.Round(mRate, 2),
                    TotalPresent = mPresent,
                    TotalAbsent = mAbsent,
                    TotalLate = mLate,
                    TotalSessions = mTotalValid
                });
            }

            return Result<DashboardSummaryDto>.Success(new DashboardSummaryDto
            {
                TodaySessionsCount = todaySessions,
                UpcomingSessionsCount = upcomingSessions,
                SemesterHealthRate = healthRate,
                Warnings = warnings,
                
                SemesterName = semester.Name,
                StartDate = semester.StartDate,
                EndDate = semester.EndDate,
                
                TotalSessions = total,
                PresentSessions = present,
                AbsentSessions = absent,
                LateSessions = late,
                CancelledSessions = cancelled,
                HolidaySessions = holiday,
                ValidSessions = valid,
                CalculatedRate = healthRate,
                
                DailyAttendance = dailyBreakdown,
                WeeklyAttendance = weeklyBreakdown,
                MonthlyAttendance = monthlyBreakdown,
                ModuleAttendance = moduleBreakdown
            });
        }
        catch (Exception ex)
        {
            return Result<DashboardSummaryDto>.Failure($"Failed to get dashboard summary: {ex.Message}");
        }
    }
}
