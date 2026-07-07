using System;
using System.ComponentModel.DataAnnotations;

namespace ACST.Domain.DTOs.Holiday;

public class HolidayDto
{
    public long Id { get; set; }
    public DateOnly HolidayDate { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CreateHolidayRequest
{
    [Required]
    public DateOnly HolidayDate { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
}

public class ImportGoogleHolidaysRequest
{
    [Required]
    public string CalendarId { get; set; } = string.Empty;

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }
}
