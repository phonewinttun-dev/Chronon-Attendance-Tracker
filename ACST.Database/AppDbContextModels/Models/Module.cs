using System;
using System.Collections.Generic;

namespace ACST.Database.AppDbContextModels.Models;

public partial class Module
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string? TeacherName { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<ClassSession> ClassSessions { get; set; } = new List<ClassSession>();

    public virtual ICollection<RecurringSchedule> RecurringSchedules { get; set; } = new List<RecurringSchedule>();
}
