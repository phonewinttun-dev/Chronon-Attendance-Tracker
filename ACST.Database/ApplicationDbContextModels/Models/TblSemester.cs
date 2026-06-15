using System;
using System.Collections.Generic;

namespace ACST.Database.ApplicationDbContextModels.Models;

public partial class TblSemester
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<TblRecurringSchedule> TblRecurringSchedules { get; set; } = new List<TblRecurringSchedule>();

    public virtual ICollection<TblSession> TblSessions { get; set; } = new List<TblSession>();
}
