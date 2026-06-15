using System;
using System.Collections.Generic;

namespace ACST.Database.ApplicationDbContextModels.Models;

public partial class TblHoliday
{
    public long Id { get; set; }

    public DateOnly HolidayDate { get; set; }

    public string Name { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }
}
