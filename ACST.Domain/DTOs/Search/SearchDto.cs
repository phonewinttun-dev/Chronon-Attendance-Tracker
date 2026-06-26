using System;

namespace ACST.Domain.DTOs.Search
{
    public class SearchDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string ModuleCode { get; set; } = null!;
        public string? TeacherName { get; set; }
    }

    public class SearchModuleDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string ModuleCode { get; set; } = null!;
        public string? TeacherName { get; set; }
        public long? SemesterId { get; set; }
        public string? SemesterName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class SearchSemesterDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class SearchClassSessionDto
    {
        public long Id { get; set; }
        public long ModuleId { get; set; }
        public string ModuleName { get; set; } = null!;
        public string ModuleCode { get; set; } = null!;
        public long SemesterId { get; set; }
        public string SemesterName { get; set; } = null!;
        public DateOnly SessionDate { get; set; }
        public DateTime StartDatetime { get; set; }
        public DateTime EndDatetime { get; set; }
        public string Status { get; set; } = null!;
        public Guid MagicLinkToken { get; set; }
        public string? GoogleEventId { get; set; }
    }
}

