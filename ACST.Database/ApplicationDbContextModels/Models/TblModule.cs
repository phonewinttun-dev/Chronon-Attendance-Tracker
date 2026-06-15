using System;
using System.Collections.Generic;

namespace ACST.Database.ApplicationDbContextModels.Models;

public partial class TblModule
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string? TeacherName { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public long? SemesterId { get; set; }

    public virtual TblSemester? Semester { get; set; }

    public virtual ICollection<TblRecurringSchedule> TblRecurringSchedules { get; set; } = new List<TblRecurringSchedule>();

    public virtual ICollection<TblSession> TblSessions { get; set; } = new List<TblSession>();
}
