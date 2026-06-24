using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ACST.Domain.DTOs.ClassSession;
using ACST.Shared;

namespace ACST.Domain.Features.ClassSessions;

public interface IClassSessionService
{
    Task<PagedResult<ClassSessionDto>> GetSessionsAsync(long? semesterId, long? moduleId, DateOnly? startDate, DateOnly? endDate, string? status, int? dayOfWeek = null, int? pageNumber = null, int? pageSize = null, string? searchTerm = null);
    Task<Result<ClassSessionDto>> GetSessionByIdAsync(long id);
    Task<Result> GenerateSessionsAsync(GenerateSessionsRequest request);
    Task<Result> UpdateSessionStatusAsync(long id, UpdateSessionStatusRequest request);
    Task<Result<string>> MarkAttendanceWithMagicLinkAsync(Guid token);
    Task<Result> UpdateSessionAsync(long id, UpdateClassSessionRequest request);
    Task<Result> DeleteSessionAsync(long id);
}
