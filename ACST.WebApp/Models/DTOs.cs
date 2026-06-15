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
