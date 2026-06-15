using System;
using System.ComponentModel.DataAnnotations;

namespace ACST.Domain.DTOs.Module;

public class ModuleDto
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string? TeacherName { get; set; }
    public long? SemesterId { get; set; }
    public string? SemesterName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateModuleRequest
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = null!;

    [MaxLength(255)]
    public string? TeacherName { get; set; }

    public long? SemesterId { get; set; }
}

public class UpdateModuleRequest
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = null!;

    [MaxLength(255)]
    public string? TeacherName { get; set; }

    public long? SemesterId { get; set; }
}
