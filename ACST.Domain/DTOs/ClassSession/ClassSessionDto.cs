using ACST.Shared;
using System;
using System.ComponentModel.DataAnnotations;

namespace ACST.Domain.DTOs.ClassSession;

public class ClassSessionDto
{
    public long Id { get; set; }
    public long RecurringScheduleId { get; set; }
    public long ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string ModuleCode { get; set; } = string.Empty;
    public long SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public DateOnly SessionDate { get; set; }
    public DateTime StartDatetime { get; set; }
    public DateTime EndDatetime { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid MagicLinkToken { get; set; }
    public string? GoogleEventId { get; set; }
}

public class GetClassSessionsRequest : PaginationRequest
{
    public long? SemesterId { get; set; }
    public long? ModuleId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Status { get; set; }
    public int? DayOfWeek { get; set; }
}

public class UpdateSessionStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}

public class GenerateSessionsRequest
{
    [Required]
    public long SemesterId { get; set; }
    
    public long? ModuleId { get; set; }

    public bool SyncWithGoogleCalendar { get; set; } = true;
}

public class UpdateClassSessionRequest
{
    [Required]
    public long ModuleId { get; set; }

    [Required]
    public DateOnly SessionDate { get; set; }

    [Required]
    public DateTime StartDatetime { get; set; }

    [Required]
    public DateTime EndDatetime { get; set; }

    [Required]
    public string Status { get; set; } = string.Empty;
}

public class BulkUpdateSessionStatusRequest
{
    [Required]
    public List<long> SessionIds { get; set; } = new();

    [Required]
    public string Status { get; set; } = string.Empty;
}

