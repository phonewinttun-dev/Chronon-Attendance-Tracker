using System;
using System.Collections.Generic;

namespace ACST.Database.AppDbContextModels.Models;

public partial class ClassSession
{
    public long Id { get; set; }

    public long RecurringScheduleId { get; set; }

    public long ModuleId { get; set; }

    public long SemesterId { get; set; }

    public DateOnly SessionDate { get; set; }

    public DateTime StartDatetime { get; set; }

    public DateTime EndDatetime { get; set; }

    /// <summary>
    /// Not Marked, Present, Absent, Cancelled, Holiday
    /// </summary>
    public string Status { get; set; } = null!;

    public Guid MagicLinkToken { get; set; }

    public string? GoogleEventId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Module Module { get; set; } = null!;

    public virtual RecurringSchedule RecurringSchedule { get; set; } = null!;

    public virtual Semester Semester { get; set; } = null!;
}
