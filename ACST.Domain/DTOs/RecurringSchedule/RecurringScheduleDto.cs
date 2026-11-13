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
    public DateOnly? EffectiveStartDate { get; set; }
    public DateOnly? EffectiveEndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateRecurringScheduleRequest : IValidatableObject
{
    [Required]
    [Range(0, 6, ErrorMessage = "DayOfWeek must be between 0 (Sunday) and 6 (Saturday).")]
    public short DayOfWeek { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }

    public DateOnly? EffectiveStartDate { get; set; }
    public DateOnly? EffectiveEndDate { get; set; }

    public bool GenerateSessions { get; set; } = true;
    public bool SyncWithGoogleCalendar { get; set; } = false;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartTime >= EndTime)
        {
            yield return new ValidationResult("Start time must be before end time.", new[] { nameof(StartTime), nameof(EndTime) });
        }

        if (EffectiveStartDate.HasValue && EffectiveEndDate.HasValue && EffectiveStartDate.Value > EffectiveEndDate.Value)
        {
            yield return new ValidationResult("Effective start date must be on or before effective end date.", new[] { nameof(EffectiveStartDate), nameof(EffectiveEndDate) });
        }
    }
}

public class UpdateRecurringScheduleRequest : IValidatableObject
{
    public long? Id { get; set; }

    [Required]
    [Range(0, 6, ErrorMessage = "DayOfWeek must be between 0 (Sunday) and 6 (Saturday).")]
    public short DayOfWeek { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }

    public DateOnly? EffectiveStartDate { get; set; }
    public DateOnly? EffectiveEndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartTime >= EndTime)
        {
            yield return new ValidationResult("Start time must be before end time.", new[] { nameof(StartTime), nameof(EndTime) });
        }

        if (EffectiveStartDate.HasValue && EffectiveEndDate.HasValue && EffectiveStartDate.Value > EffectiveEndDate.Value)
        {
            yield return new ValidationResult("Effective start date must be on or before effective end date.", new[] { nameof(EffectiveStartDate), nameof(EffectiveEndDate) });
        }
    }
}
