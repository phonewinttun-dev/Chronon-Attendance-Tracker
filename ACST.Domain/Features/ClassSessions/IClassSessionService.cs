using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ACST.Domain.DTOs.ClassSession;
using ACST.Shared;

namespace ACST.Domain.Features.ClassSessions;

public interface IClassSessionService
{
    Task<Result<IEnumerable<ClassSessionDto>>> GetSessionsAsync(long? semesterId, long? moduleId, DateOnly? startDate, DateOnly? endDate, string? status);
    Task<Result<ClassSessionDto>> GetSessionByIdAsync(long id);
    Task<Result> GenerateSessionsAsync(GenerateSessionsRequest request);
    Task<Result> UpdateSessionStatusAsync(long id, UpdateSessionStatusRequest request);
    Task<Result<string>> MarkAttendanceWithMagicLinkAsync(Guid token);
}
