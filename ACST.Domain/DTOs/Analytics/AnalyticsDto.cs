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
    public double AttendanceRate { get; set; }
    public int TotalPresent { get; set; }
    public int TotalAbsent { get; set; }
    public int TotalLate { get; set; }
    public int TotalSessions { get; set; }
}

public class DashboardSummaryDto
{
    public int UpcomingSessionsCount { get; set; }
    public int TodaySessionsCount { get; set; }
    public double SemesterHealthRate { get; set; }
    public List<string> Warnings { get; set; } = new();
}
