using System;
using System.ComponentModel.DataAnnotations;

namespace ACST.Domain.DTOs.Semester;

public class SemesterDto
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSemesterRequest
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = null!;

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }
}

public class UpdateSemesterRequest
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = null!;

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }
}
