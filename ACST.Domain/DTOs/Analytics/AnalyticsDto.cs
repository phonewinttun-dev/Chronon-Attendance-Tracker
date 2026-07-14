using System;
using System.Collections.Generic;

namespace ACST.Domain.DTOs.Analytics;

public class OverallAnalyticsDto
{
    public double OverallRate { get; set; }
    public int TotalPresent { get; set; }
    public int TotalAbsent { get; set; }
    public int TotalLate { get; set; }
    public int TotalSessions { get; set; }
    public int ExcludedHolidaysCount { get; set; }
    public int ExcludedCancelledCount { get; set; }
}

public class ModuleAnalyticsDto
{
    public long ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string? TeacherName { get; set; }
    public double AttendanceRate { get; set; }
    public int TotalPresent { get; set; }
    public int TotalAbsent { get; set; }
    public int TotalLate { get; set; }
    public int TotalSessions { get; set; }
    public int NotMarked { get; set; }
    public int Cancelled { get; set; }
    public int Holiday { get; set; }
}

public class DashboardSummaryDto
{
    public int UpcomingSessionsCount { get; set; }
    public int TodaySessionsCount { get; set; }
    public double SemesterHealthRate { get; set; }
    public List<string> Warnings { get; set; } = new();

    // Semester Details
    public string SemesterName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    // Reconciliation Data
    public int TotalSessions { get; set; }
    public int PresentSessions { get; set; }
    public int AbsentSessions { get; set; }
    public int LateSessions { get; set; }
    public int CancelledSessions { get; set; }
    public int HolidaySessions { get; set; }
    public int NotMarkedSessions { get; set; }
    public int ValidSessions { get; set; }
    public double CalculatedRate { get; set; }
    public double? TodayAttendanceRate { get; set; }

    // Breakdowns
    public List<DailyAttendanceDto> DailyAttendance { get; set; } = new();
    public List<WeeklyAttendanceDto> WeeklyAttendance { get; set; } = new();
    public List<MonthlyAttendanceDto> MonthlyAttendance { get; set; } = new();
    public List<ModuleAnalyticsDto> ModuleAttendance { get; set; } = new();
}

public class DailyAttendanceDto
{
    public string DayOfWeek { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public int Present { get; set; }
    public int Absent { get; set; }
    public int Late { get; set; }
    public int Cancelled { get; set; }
    public int Holiday { get; set; }
    public int NotMarked { get; set; }
    public int ValidSessions { get; set; }
    public double AttendanceRate { get; set; }
}

public class WeeklyAttendanceDto
{
    public int WeekNumber { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public int TotalSessions { get; set; }
    public int Present { get; set; }
    public int Absent { get; set; }
    public int Late { get; set; }
    public int Cancelled { get; set; }
    public int Holiday { get; set; }
    public int NotMarked { get; set; }
    public int ValidSessions { get; set; }
    public double AttendanceRate { get; set; }
}

public class MonthlyAttendanceDto
{
    public string MonthName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalSessions { get; set; }
    public int Present { get; set; }
    public int Absent { get; set; }
    public int Late { get; set; }
    public int Cancelled { get; set; }
    public int Holiday { get; set; }
    public int NotMarked { get; set; }
    public int ValidSessions { get; set; }
    public double AttendanceRate { get; set; }
}

public class DashboardDailyWeeklyDto
{
    public List<DailyAttendanceDto> DailyAttendance { get; set; } = new();
    public List<WeeklyAttendanceDto> WeeklyAttendance { get; set; } = new();
    public List<MonthlyAttendanceDto> MonthlyAttendance { get; set; } = new();
}
