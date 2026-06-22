using System;
using System.Collections.Generic;

namespace ACST.Database.ApplicationDbContextModels.Models;

public partial class TblSession
{
    public long Id { get; set; }

    public long RecurringScheduleId { get; set; }

    public long ModuleId { get; set; }

    public long SemesterId { get; set; }

    public DateOnly SessionDate { get; set; }

    public DateTime StartDatetime { get; set; }

    public DateTime EndDatetime { get; set; }

    public string Status { get; set; } = null!;

    public Guid MagicLinkToken { get; set; }

    public string? GoogleEventId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual TblModule Module { get; set; } = null!;

    public virtual TblRecurringSchedule RecurringSchedule { get; set; } = null!;

    public virtual TblSemester Semester { get; set; } = null!;
}
