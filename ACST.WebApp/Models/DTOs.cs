using System;
using System.Collections.Generic;

namespace ACST.WebApp.Models;

// --- DTOs ---

public class SemesterDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}

public class ModuleDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public long? SemesterId { get; set; }
    public string? SemesterName { get; set; }
}

public class HolidayDto
{
    public long Id { get; set; }
    public DateOnly HolidayDate { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ClassSessionDto
{
    public long Id { get; set; }
    public long ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public DateOnly SessionDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartDatetime { get; set; }
    public DateTime EndDatetime { get; set; }
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
    public int ValidSessions { get; set; }
    public double CalculatedRate { get; set; }

    // Breakdowns
    public List<DailyAttendanceDto> DailyAttendance { get; set; } = new();
    public List<WeeklyAttendanceDto> WeeklyAttendance { get; set; } = new();
    public List<MonthlyAttendanceDto> MonthlyAttendance { get; set; } = new();
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
    public int ValidSessions { get; set; }
    public double AttendanceRate { get; set; }
}

public class RecurringScheduleDto
{
    public long Id { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string SemesterName { get; set; } = string.Empty;
    public short DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}

// --- Requests ---

public class CreateSemesterRequest
{
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}

public class CreateModuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public long? SemesterId { get; set; }
}

public class CreateRecurringScheduleRequest
{
    public short DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}

// --- API Wrappers ---

public class ApiResult<T>
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}

public class ApiResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}
