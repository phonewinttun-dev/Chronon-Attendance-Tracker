using System;
using System.Collections.Generic;

namespace ACST.Database.AppDbContextModels.Models;

public partial class Holiday
{
    public long Id { get; set; }

    public DateOnly HolidayDate { get; set; }

    public string Name { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }
}
