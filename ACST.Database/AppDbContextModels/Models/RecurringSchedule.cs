using System;
using System.Collections.Generic;

namespace ACST.Database.AppDbContextModels.Models;

public partial class RecurringSchedule
{
    public long Id { get; set; }

    public long ModuleId { get; set; }

    public long SemesterId { get; set; }

    /// <summary>
    /// 0=Sunday ... 6=Saturday
    /// </summary>
    public short DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<ClassSession> ClassSessions { get; set; } = new List<ClassSession>();

    public virtual Module Module { get; set; } = null!;

    public virtual Semester Semester { get; set; } = null!;
}
