using System;
using System.ComponentModel.DataAnnotations;

namespace ACST.Domain.DTOs.RecurringSchedule;

public class RecurringScheduleDto
{
    public long Id { get; set; }
    public long ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public long SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public short DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateRecurringScheduleRequest
{
    [Required]
    [Range(0, 6, ErrorMessage = "DayOfWeek must be between 0 (Sunday) and 6 (Saturday).")]
    public short DayOfWeek { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }
}

public class UpdateRecurringScheduleRequest
{
    public long? Id { get; set; }

    [Required]
    [Range(0, 6, ErrorMessage = "DayOfWeek must be between 0 (Sunday) and 6 (Saturday).")]
    public short DayOfWeek { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }
}
