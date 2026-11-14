using System;
using System.Collections.Generic;
using System.Linq;

namespace ACST.WebApp.Models;

public enum ScheduleLifecycleStatus
{
    FullTerm,   // No custom dates specified; active throughout entire semester
    Active,     // Custom dates specified and today is within [EffectiveStartDate, EffectiveEndDate]
    Upcoming,   // Custom dates specified and today is strictly before EffectiveStartDate
    Concluded   // Custom dates specified and today is strictly after EffectiveEndDate
}

public class ScheduleEntryFormModel
{
    public long? Id { get; set; }
    public short DayOfWeek { get; set; } = 1; // Default Monday
    public TimeOnly StartTime { get; set; } = new(9, 0);
    public TimeOnly EndTime { get; set; } = new(11, 0);
    public bool IsCustomRange { get; set; } = false;
    public DateOnly? EffectiveStartDate { get; set; }
    public DateOnly? EffectiveEndDate { get; set; }

    public static ScheduleEntryFormModel FromCreateRequest(CreateRecurringScheduleRequest req)
    {
        return new ScheduleEntryFormModel
        {
            DayOfWeek = req.DayOfWeek,
            StartTime = req.StartTime,
            EndTime = req.EndTime,
            EffectiveStartDate = req.EffectiveStartDate,
            EffectiveEndDate = req.EffectiveEndDate,
            IsCustomRange = req.EffectiveStartDate.HasValue || req.EffectiveEndDate.HasValue
        };
    }

    public static ScheduleEntryFormModel FromUpdateRequest(UpdateRecurringScheduleRequest req)
    {
        return new ScheduleEntryFormModel
        {
            Id = req.Id,
            DayOfWeek = req.DayOfWeek,
            StartTime = req.StartTime,
            EndTime = req.EndTime,
            EffectiveStartDate = req.EffectiveStartDate,
            EffectiveEndDate = req.EffectiveEndDate,
            IsCustomRange = req.EffectiveStartDate.HasValue || req.EffectiveEndDate.HasValue
        };
    }

    public static ScheduleEntryFormModel FromDto(RecurringScheduleDto dto)
    {
        return new ScheduleEntryFormModel
        {
            Id = dto.Id,
            DayOfWeek = dto.DayOfWeek,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            EffectiveStartDate = dto.EffectiveStartDate,
            EffectiveEndDate = dto.EffectiveEndDate,
            IsCustomRange = dto.EffectiveStartDate.HasValue || dto.EffectiveEndDate.HasValue
        };
    }

    public CreateRecurringScheduleRequest ToCreateRequest()
    {
        return new CreateRecurringScheduleRequest
        {
            DayOfWeek = DayOfWeek,
            StartTime = StartTime,
            EndTime = EndTime,
            EffectiveStartDate = IsCustomRange ? EffectiveStartDate : null,
            EffectiveEndDate = IsCustomRange ? EffectiveEndDate : null
        };
    }

    public UpdateRecurringScheduleRequest ToUpdateRequest()
    {
        return new UpdateRecurringScheduleRequest
        {
            Id = Id,
            DayOfWeek = DayOfWeek,
            StartTime = StartTime,
            EndTime = EndTime,
            EffectiveStartDate = IsCustomRange ? EffectiveStartDate : null,
            EffectiveEndDate = IsCustomRange ? EffectiveEndDate : null
        };
    }
}

public class ScheduleValidationState
{
    public bool HasTimeError { get; set; }
    public string? TimeErrorMessage { get; set; }
    public bool HasDateError { get; set; }
    public string? DateErrorMessage { get; set; }
    public bool HasConflict { get; set; }
    public string? ConflictMessage { get; set; }

    public bool HasErrors => HasTimeError || HasDateError || HasConflict;
}

public static class ScheduleUiHelper
{
    public static ScheduleLifecycleStatus GetLifecycleStatus(DateOnly? start, DateOnly? end, DateOnly? today = null)
    {
        if (!start.HasValue && !end.HasValue)
            return ScheduleLifecycleStatus.FullTerm;

        var current = today ?? DateOnly.FromDateTime(DateTime.Today);

        if (end.HasValue && current > end.Value)
            return ScheduleLifecycleStatus.Concluded;

        if (start.HasValue && current < start.Value)
            return ScheduleLifecycleStatus.Upcoming;

        return ScheduleLifecycleStatus.Active;
    }

    public static string FormatEffectiveRangeBadge(DateOnly? start, DateOnly? end, DateOnly? today = null)
    {
        if (!start.HasValue && !end.HasValue)
            return string.Empty;

        var status = GetLifecycleStatus(start, end, today);

        return status switch
        {
            ScheduleLifecycleStatus.Upcoming when start.HasValue => $"STARTS {start.Value:MMM dd}",
            ScheduleLifecycleStatus.Concluded when end.HasValue => $"ENDED {end.Value:MMM dd}",
            ScheduleLifecycleStatus.Active when start.HasValue && end.HasValue => $"ACTIVE ({start.Value:MMM dd} - {end.Value:MMM dd})",
            ScheduleLifecycleStatus.Active when start.HasValue => $"ACTIVE (From {start.Value:MMM dd})",
            ScheduleLifecycleStatus.Active when end.HasValue => $"ACTIVE (Until {end.Value:MMM dd})",
            _ => string.Empty
        };
    }

    public static ScheduleValidationState ValidateSchedule(
        TimeOnly startTime,
        TimeOnly endTime,
        bool isCustomRange,
        DateOnly? effectiveStartDate,
        DateOnly? effectiveEndDate,
        DateOnly? semesterStartDate,
        DateOnly? semesterEndDate)
    {
        var state = new ScheduleValidationState();

        if (endTime <= startTime)
        {
            state.HasTimeError = true;
            state.TimeErrorMessage = "End time must be after start time.";
        }

        if (isCustomRange)
        {
            if (effectiveStartDate.HasValue && effectiveEndDate.HasValue && effectiveStartDate.Value > effectiveEndDate.Value)
            {
                state.HasDateError = true;
                state.DateErrorMessage = "Effective start date must be on or before effective end date.";
            }
            else if (semesterStartDate.HasValue && effectiveStartDate.HasValue && effectiveStartDate.Value < semesterStartDate.Value)
            {
                state.HasDateError = true;
                state.DateErrorMessage = $"Effective start date cannot be before semester start ({semesterStartDate.Value:MMM dd, yyyy}).";
            }
            else if (semesterEndDate.HasValue && effectiveEndDate.HasValue && effectiveEndDate.Value > semesterEndDate.Value)
            {
                state.HasDateError = true;
                state.DateErrorMessage = $"Effective end date cannot be after semester end ({semesterEndDate.Value:MMM dd, yyyy}).";
            }
            else if (semesterEndDate.HasValue && effectiveStartDate.HasValue && effectiveStartDate.Value > semesterEndDate.Value)
            {
                state.HasDateError = true;
                state.DateErrorMessage = $"Effective start date cannot be after semester end ({semesterEndDate.Value:MMM dd, yyyy}).";
            }
        }

        return state;
    }

    public static List<ScheduleValidationState> ValidateAndCheckConflicts(
        IReadOnlyList<ScheduleEntryFormModel> schedules,
        DateOnly? semesterStartDate,
        DateOnly? semesterEndDate)
    {
        var states = new List<ScheduleValidationState>();

        for (int i = 0; i < schedules.Count; i++)
        {
            var sch = schedules[i];
            var state = ValidateSchedule(
                sch.StartTime,
                sch.EndTime,
                sch.IsCustomRange,
                sch.EffectiveStartDate,
                sch.EffectiveEndDate,
                semesterStartDate,
                semesterEndDate);
            states.Add(state);
        }

        // Intra-module schedule conflict check
        for (int i = 0; i < schedules.Count; i++)
        {
            var a = schedules[i];
            for (int j = i + 1; j < schedules.Count; j++)
            {
                var b = schedules[j];

                if (a.DayOfWeek == b.DayOfWeek)
                {
                    // Check time overlap: StartA < EndB && StartB < EndA
                    bool timesOverlap = a.StartTime < b.EndTime && b.StartTime < a.EndTime;

                    if (timesOverlap)
                    {
                        // Check date range overlap
                        var aStart = (a.IsCustomRange && a.EffectiveStartDate.HasValue) ? a.EffectiveStartDate.Value : (semesterStartDate ?? DateOnly.MinValue);
                        var aEnd = (a.IsCustomRange && a.EffectiveEndDate.HasValue) ? a.EffectiveEndDate.Value : (semesterEndDate ?? DateOnly.MaxValue);

                        var bStart = (b.IsCustomRange && b.EffectiveStartDate.HasValue) ? b.EffectiveStartDate.Value : (semesterStartDate ?? DateOnly.MinValue);
                        var bEnd = (b.IsCustomRange && b.EffectiveEndDate.HasValue) ? b.EffectiveEndDate.Value : (semesterEndDate ?? DateOnly.MaxValue);

                        bool datesOverlap = aStart <= bEnd && bStart <= aEnd;

                        if (datesOverlap)
                        {
                            states[i].HasConflict = true;
                            states[i].ConflictMessage = $"Overlaps with schedule #{j + 1} ({GetDayOfWeekName(b.DayOfWeek)} {b.StartTime:hh:mm tt}-{b.EndTime:hh:mm tt}).";

                            states[j].HasConflict = true;
                            states[j].ConflictMessage = $"Overlaps with schedule #{i + 1} ({GetDayOfWeekName(a.DayOfWeek)} {a.StartTime:hh:mm tt}-{a.EndTime:hh:mm tt}).";
                        }
                    }
                }
            }
        }

        return states;
    }

    public static string GetDayOfWeekName(short dayOfWeek) => dayOfWeek switch
    {
        1 => "Monday",
        2 => "Tuesday",
        3 => "Wednesday",
        4 => "Thursday",
        5 => "Friday",
        6 => "Saturday",
        0 => "Sunday",
        _ => "Unknown"
    };
}
